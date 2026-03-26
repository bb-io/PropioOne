using Apps.PropioOne.Models.Edit;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using RestSharp;

namespace Apps.PropioOne.Actions
{
    [ActionList("Editing")]
    public class EditActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
    {
        [BlueprintActionDefinition(BlueprintAction.EditFile)]
        [Action("Edit", Description = "Edit a translation using Propio APE. Assumes translated content was produced earlier.")]
        public async Task<EditFileResponse> Edit([ActionParameter] EditFileRequest input)
        {
            await using var stream = await fileManagement.DownloadAsync(input.File);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var contentString = System.Text.Encoding.UTF8.GetString(ms.ToArray());

            var content = Transformation.Parse(contentString, input.File.Name)
                ?? throw new PluginApplicationException(
                    "Failed to parse bilingual file. Please send the file to the team for inspection.");

            var srcLang = input.SourceLanguage ?? content.SourceLanguage;

            var trgLang = input.TargetLanguage ?? content.TargetLanguage;

            var items = GetSegmentsToProcess(content)
                .Select(x => new SegmentWorkItem(
                    x.unit,
                    x.segment,
                    x.segment.GetSource(),
                    x.segment.GetTarget()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Source) && !string.IsNullOrWhiteSpace(x.Target))
                .ToList();

            int reviewed = 0;
            int updated = 0;

            if (items.Count == 0)
            {
                var passthroughStream = BuildOutputStream(content, contentString, input.OutputFileHandling);
                var passthroughUploaded = await fileManagement.UploadAsync(
                    passthroughStream, input.File.ContentType, input.File.Name);

                return new EditFileResponse
                {
                    File = passthroughUploaded,
                    TotalSegmentsReviewed = 0,
                    TotalSegmentsUpdated = 0
                };
            }

            const int batchSize = 50;
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();

                var request = new RestRequest("/api/v1/PostEditing/GetApeSegments", Method.Post);
                request.AddJsonBody(new GetApeSegmentsRequest
                {
                    OriginalText = batch.Select(b => b.Source).ToList(),
                    TranslatedText = batch.Select(b => b.Target).ToList(),
                    Domain = input.Domain ?? "General Vocabulary",
                    SourceLanguage = srcLang,
                    TargetLanguage = trgLang,
                    SourceFile = "",
                    TargetFile = ""
                });

                var resp = await Client.ExecuteWithErrorHandling<GetApeSegmentsResponse>(request);

                var edits = FlattenOrdered(resp);

                var countToApply = Math.Min(batch.Count, edits.Count);

                for (int j = 0; j < batch.Count; j++)
                {
                    var item = batch[j];
                    var editedText = j < countToApply ? edits[j].Ape : null;

                    reviewed++;

                    if (!string.IsNullOrWhiteSpace(editedText) &&
                        !string.Equals(editedText, item.Target, StringComparison.Ordinal))
                    {
                        item.Segment.SetTarget(editedText);
                        updated++;

                        item.Unit.Notes.Add(new Note("Edited by Proprio APE") { Reference = item.Segment.Id });
                    }
                    else
                    {
                        item.Unit.Notes.Add(new Note("Reviewed by Proprio APE (no changes)") { Reference = item.Segment.Id });
                    }

                    item.Segment.State = SegmentState.Reviewed;
                    item.Unit.Provenance.Review.Tool = "Proprio APE";
                }
            }

            Stream outputStream = BuildOutputStream(content, contentString, input.OutputFileHandling);

            var uploaded = await fileManagement.UploadAsync(outputStream, input.File.ContentType, input.File.Name);

            return new EditFileResponse
            {
                File = uploaded,
                TotalSegmentsReviewed = reviewed,
                TotalSegmentsUpdated = updated
            };
        }

        [BlueprintActionDefinition(BlueprintAction.EditText)]
        [Action("Edit text", Description = "Post-edit already translated text")]
        public async Task<EditTextResponse> EditText([ActionParameter] EditTextRequest input)
        {
            if (string.IsNullOrWhiteSpace(input.SourceLanguage))
                throw new PluginMisconfigurationException("Source language is required for this action (e.g., en-US).");

            if (string.IsNullOrWhiteSpace(input.TargetLanguage))
                throw new PluginMisconfigurationException("Target language is required for this action (e.g., de-DE).");

            var request = new RestRequest("/api/v1/PostEditing/GetApeSegments", Method.Post);
            request.AddJsonBody(new GetApeSegmentsRequest
            {
                OriginalText = new() { input.SourceText },
                TranslatedText = new() { input.TargetText },
                Domain = input.Domain ?? "General Vocabulary",
                SourceLanguage = input.SourceLanguage,
                TargetLanguage = input.TargetLanguage,
                SourceFile = "",
                TargetFile = ""
            });

            var resp = await Client.ExecuteWithErrorHandling<GetApeSegmentsResponse>(request);
            var edited = FlattenOrdered(resp).FirstOrDefault()?.Ape;

            return new EditTextResponse
            {
                EditedText = string.IsNullOrWhiteSpace(edited) ? input.TargetText : edited
            };
        }

        private static IEnumerable<(Unit unit, Segment segment)> GetSegmentsToProcess(Transformation transformation)
        {
            foreach (var unit in transformation.GetUnits())
            {
                if (unit.IsInitial || unit.State == SegmentState.Final)
                    continue;

                foreach (var segment in unit.Segments)
                {
                    if (segment.IsIgnorbale || segment.State == SegmentState.Final)
                        continue;

                    yield return (unit, segment);
                }
            }
        }

        private sealed record SegmentWorkItem(Unit Unit, Segment Segment, string Source, string Target);

        public List<ApeSegmentDto> FlattenOrdered(GetApeSegmentsResponse? response)
        {
            if (response?.Ape == null || response.Ape.Count == 0)
                return [];


            var indexed = new List<(int Index, ApeSegmentDto Segment)>();

            foreach (var dict in response.Ape)
            {
                if (dict == null) continue;

                foreach (var kv in dict)
                {
                    if (!int.TryParse(kv.Key, out var index))
                        continue;

                    if (kv.Value == null) continue;

                    indexed.Add((index, kv.Value));
                }
            }

            return indexed
                .OrderBy(x => x.Index)
                .Select(x => x.Segment)
                .ToList();
        }

        private static Stream BuildOutputStream(Transformation content, string originalFileString, string outputHandling)
        {
            outputHandling ??= string.Empty;

            if (string.Equals(outputHandling, "original", StringComparison.OrdinalIgnoreCase))
            {
                if (Xliff2Serializer.IsXliff2(originalFileString))
                {
                    return Xliff2Serializer.Serialize(content).ToStream();
                }

                try
                {
                    var target = content.Target();
                    return target.Serialize().ToStream();
                }
                catch
                {
                    return content.Serialize().ToStream();
                }
            }

            if (string.Equals(outputHandling, "xliff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(outputHandling, "xliff2", StringComparison.OrdinalIgnoreCase))
            {
                return Xliff2Serializer.Serialize(content).ToStream();
            }

            return content.Serialize().ToStream();
        }
    }
}
