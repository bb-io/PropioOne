using Apps.PropioOne.Models.Review;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using RestSharp;
using System.Text;

namespace Apps.PropioOne.Actions
{
    [ActionList("Review")]
    public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
    {
        [BlueprintActionDefinition(BlueprintAction.ReviewText)]
        [Action("Review text", Description = "Reviews translation quality using Basic quality estimation (source/target text are sent as .txt files).")]
        public async Task<ReviewTextResponse> ReviewText([ActionParameter] ReviewTextRequest input)
        {
            if (string.IsNullOrWhiteSpace(input.SourceText))
                throw new PluginMisconfigurationException("Source text is required.");

            if (string.IsNullOrWhiteSpace(input.TargetText))
                throw new PluginMisconfigurationException("Target text is required.");

            if (string.IsNullOrWhiteSpace(input.SourceLanguage))
                throw new PluginMisconfigurationException("Source language is required.");

            if (string.IsNullOrWhiteSpace(input.TargetLanguage))
                throw new PluginMisconfigurationException("Target language is required.");

            var domain = string.IsNullOrWhiteSpace(input.Domain) ? "General Vocabulary" : input.Domain;

            var sourceBytes = Encoding.UTF8.GetBytes(input.SourceText);
            var targetBytes = Encoding.UTF8.GetBytes(input.TargetText);

            var request = new RestRequest("/api/v1/QualityEstimation/BasicQEScore", Method.Post)
            {
                AlwaysMultipartFormData = true
            };

            request.AddFile("targetfile", targetBytes, "target.txt");
            request.AddFile("sourcefile", sourceBytes, "source.txt");

            request.AddParameter("domain", domain);
            request.AddParameter("sourceLanguage", input.SourceLanguage);
            request.AddParameter("targetLanguage", input.TargetLanguage);

            var api = await Client.ExecuteWithErrorHandling<BasicQeApiResponse>(request);

            var avg = api.BasicQEscoreAVG ?? 0.0;
            var normalized = avg > 1.0 ? avg / 100.0 : avg;

            return new ReviewTextResponse
            {
                Score = (float)normalized
            };
        }

        [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
        [Action("Review", Description = "Reviews a translated file using Basic QE. Supports blackbird (segment-based) and propio (file-to-file) strategies.")]
        public async Task<QualityEstimationResponse> Review([ActionParameter] QualityEstimationRequest input)
        {
            if (input.File == null)
                throw new PluginMisconfigurationException("Source file is required.");

            var strategy = (input.ReviewStrategy ?? "blackbird").Trim().ToLowerInvariant();
            if (strategy == "propio")
                return await ReviewWithPropio(input);

            return await ReviewWithBlackbird(input);
        }

        private async Task<QualityEstimationResponse> ReviewWithBlackbird(QualityEstimationRequest input)
        {
            var threshold = input.Threshold ?? 0.8;
            if (threshold < 0 || threshold > 1)
                throw new PluginMisconfigurationException("Threshold must be in range 0..1.");

            using var stream = await fileManagement.DownloadAsync(input.File!);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            Transformation content;
            try
            {
                content = await Transformation.Parse(ms, input.File!.Name);
            }
            catch
            {
                var contentStringFallback = Encoding.UTF8.GetString(ms.ToArray());
                content = Transformation.Parse(contentStringFallback, input.File!.Name)
                          ?? throw new PluginApplicationException("Could not parse this file as XLIFF/Transformation.");
            }

            content.SourceLanguage ??= input.SourceLanguage;
            content.TargetLanguage ??= input.TargetLanguage;

            if (string.IsNullOrWhiteSpace(content.SourceLanguage))
                throw new PluginMisconfigurationException("Source language is not defined. Provide SourceLanguage.");
            if (string.IsNullOrWhiteSpace(content.TargetLanguage))
                throw new PluginMisconfigurationException("Target language is not defined. Provide TargetLanguage.");

            var srcLang = content.SourceLanguage!;
            var trgLang = content.TargetLanguage!;
            var domain = string.IsNullOrWhiteSpace(input.Domain) ? "General Vocabulary" : input.Domain;

            int processedSegmentsCount = 0;
            int finalizedSegmentsCount = 0;
            int underThresholdCount = 0;
            double totalScore = 0.0;

            static string RenderLine(List<LineElement>? line) =>
                line == null || line.Count == 0 ? string.Empty : string.Concat(line.Select(e => e.Render()));

            bool SegmentFilter(Segment s)
            {
                if (s == null) return false;
                if (s.IsIgnorbale) return false;
                if (s.State == SegmentState.Final) return false;

                return true;
            }

            async Task<double> EstimateSegmentScore(string source, string target)
            {
                var request = new RestRequest("/api/v1/QualityEstimation/BasicQEScore", Method.Post)
                {
                    AlwaysMultipartFormData = true
                };

                var sourceBytes = Encoding.UTF8.GetBytes(source);
                var targetBytes = Encoding.UTF8.GetBytes(target);

                request.AddFile("sourcefile", sourceBytes, "source.txt");
                request.AddFile("targetfile", targetBytes, "target.txt");

                request.AddParameter("domain", domain);
                request.AddParameter("sourceLanguage", srcLang);
                request.AddParameter("targetLanguage", trgLang);

                var api = await Client.ExecuteWithErrorHandling<BasicQeApiResponse>(request);
                var avg = api.BasicQEscoreAVG ?? 0.0;

                return avg > 1.0 ? avg / 100.0 : avg;
            }

            var units = content.GetUnits().ToList();

            foreach (var unitChunk in units.Chunk(10))
            {
                foreach (var unit in unitChunk)
                {
                    double unitScoreSum = 0.0;
                    int unitCount = 0;

                    foreach (var segment in unit.Segments.Where(SegmentFilter))
                    {
                        var src = RenderLine(segment.Source);
                        var trg = RenderLine(segment.Target);

                        var score = await EstimateSegmentScore(src, trg);

                        processedSegmentsCount++;
                        totalScore += score;

                        unitScoreSum += score;
                        unitCount++;

                        if (score >= threshold)
                        {
                            segment.State = SegmentState.Final;
                            finalizedSegmentsCount++;
                        }
                        else
                        {
                            underThresholdCount++;
                        }
                    }

                    if (unitCount > 0)
                    {
                        unit.Quality.ProfileReference = "https://tgw.propio-ls.com/api/v1/QualityEstimation/BasicQEScore";
                        unit.Quality.ScoreThreshold = threshold;
                        unit.Quality.Score = (float)(unitScoreSum / unitCount);
                    }
                }
            }

            Stream streamResult;
            var contentString = Encoding.UTF8.GetString(ms.ToArray());

            if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (Xliff1Serializer.IsXliff1(contentString))
                {
                    var xliff1String = Xliff1Serializer.Serialize(content);
                    streamResult = xliff1String.ToStream();
                }
                else
                {
                    var targetContent = content.Target();
                    streamResult = targetContent.Serialize().ToStream();
                }
            }
            else
            {
                streamResult = content.Serialize().ToStream();
            }

            var finalFile = await fileManagement.UploadAsync(
                streamResult,
                input.File!.ContentType ?? MediaTypes.Xliff,
                input.File!.Name);

            var avgMetric = processedSegmentsCount > 0 ? (float)(totalScore / processedSegmentsCount) : 0f;
            var pctUnder = processedSegmentsCount > 0 ? (float)underThresholdCount / processedSegmentsCount : 0f;

            return new QualityEstimationResponse
            {
                File = finalFile,
                TotalSegmentsProcessed = processedSegmentsCount,
                TotalSegmentsFinalized = finalizedSegmentsCount,
                TotalSegmentsUnderThreshhold = underThresholdCount,
                AverageMetric = avgMetric,
                PercentageSegmentsUnderThreshhold = pctUnder
            };
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream s)
        {
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            return ms.ToArray();
        }

        private static string SanitizeMultipartFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);

            var ext = Path.GetExtension(fileName);
            var baseName = Path.GetFileNameWithoutExtension(fileName);

            baseName = new string(baseName.Where(c => !char.IsControl(c)).ToArray());
            baseName = baseName.Replace("\"", "'").Replace("\\", "_");

            foreach (var ch in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(ch, '_');

            baseName = baseName.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "file";

            return baseName + ext;
        }

        private async Task<QualityEstimationResponse> ReviewWithPropio(QualityEstimationRequest input)
        {
            if (input.File == null)
                throw new PluginMisconfigurationException("Source file is required.");

            if (input.TargetFile == null)
                throw new PluginMisconfigurationException("For 'propio' strategy you must provide TargetFile.");

            if (string.IsNullOrWhiteSpace(input.SourceLanguage) || string.IsNullOrWhiteSpace(input.TargetLanguage))
                throw new PluginMisconfigurationException("Source and target language must be specified.");

            var domain = string.IsNullOrWhiteSpace(input.Domain) ? "General Vocabulary" : input.Domain;

            byte[] sourceBytes;
            byte[] targetBytes;

            using (var s = await fileManagement.DownloadAsync(input.File))
                sourceBytes = await ReadAllBytesAsync(s);

            using (var s = await fileManagement.DownloadAsync(input.TargetFile))
                targetBytes = await ReadAllBytesAsync(s);

            var request = new RestRequest("/api/v1/QualityEstimation/BasicQEScore", Method.Post)
            {
                AlwaysMultipartFormData = true
            };

            request.AddFile("sourcefile", sourceBytes, SanitizeMultipartFileName(input.File.Name));
            request.AddFile("targetfile", targetBytes, SanitizeMultipartFileName(input.TargetFile.Name));

            request.AddParameter("domain", domain);
            request.AddParameter("sourceLanguage", input.SourceLanguage);
            request.AddParameter("targetLanguage", input.TargetLanguage);

            var api = await Client.ExecuteWithErrorHandling<BasicQeApiResponse>(request);

            var avg = api.BasicQEscoreAVG ?? 0.0;

            return new QualityEstimationResponse
            {
                File = input.TargetFile,
                TotalSegmentsProcessed = 0,
                TotalSegmentsFinalized = 0,
                TotalSegmentsUnderThreshhold = 0,
                AverageMetric = (float)avg,
                PercentageSegmentsUnderThreshhold = 0f
            };
        }
    }
}
