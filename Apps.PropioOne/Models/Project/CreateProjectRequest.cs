using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project
{
    public class CreateProjectRequest
    {
        [JsonProperty("projectType")]
        public string ProjectType { get; set; } = default!;

        [JsonProperty("translationFileType")]
        public string TranslationFileType { get; set; } = default!;

        [JsonProperty("projectName")]
        public string ProjectName { get; set; } = default!;

        [JsonProperty("notes")]
        public string? Notes { get; set; }

        [JsonProperty("requestedDueDate")]
        public DateTime? RequestedDueDate { get; set; }

        [JsonProperty("sourceLanguage")]
        public string SourceLanguage { get; set; } = default!;

        [JsonProperty("sourceFiles")]
        public IEnumerable<SourceFileRequestModel> SourceFiles { get; set; }
            = Enumerable.Empty<SourceFileRequestModel>();

        [JsonProperty("attributes")]
        public IEnumerable<ProjectAttribute>? Attributes { get; set; }

        [JsonProperty("projectOriginator")]
        public string? ProjectOriginator { get; set; }

        [JsonProperty("deliverableFormats")]
        public IEnumerable<string>? DeliverableFormats { get; set; }

        [JsonProperty("workflowName")]
        public string? WorkflowName { get; set; }

        [JsonProperty("createdBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("requesterName")]
        public string? RequesterName { get; set; }
    }

    public class SourceFileRequestModel
    {
        [JsonProperty("sourceFileNumber")]
        public int SourceFileNumber { get; set; }

        [JsonProperty("targetLanguages")]
        public IEnumerable<string> TargetLanguages { get; set; } = Enumerable.Empty<string>();

        [JsonProperty("pageCount")]
        public int? PageCount { get; set; }
    }

    public class ProjectAttribute
    {
        [JsonProperty("name")]
        public string Name { get; set; } = default!;

        [JsonProperty("value")]
        public string Value { get; set; } = default!;
    }
}
