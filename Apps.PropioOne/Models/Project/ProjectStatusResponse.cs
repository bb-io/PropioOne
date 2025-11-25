using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project
{
    public class ProjectStatusResponse
    {
        [JsonProperty("projectNumber")]
        [Display("Project number")]
        public int ProjectNumber { get; set; }

        [JsonProperty("tmsReferenceNumber")]
        [Display("Tms reference number")]
        public string? TmsReferenceNumber { get; set; }

        [JsonProperty("customerNumber")]
        [Display("Customer number")]
        public int CustomerNumber { get; set; }

        [JsonProperty("projectType")]
        [Display("Project type")]
        public string ProjectType { get; set; } = default!;

        [JsonProperty("projectName")]
        [Display("Project name")]
        public string ProjectName { get; set; } = default!;

        [JsonProperty("projectStatus")]
        [Display("Project status")]
        public string ProjectStatus { get; set; } = default!;

        [JsonProperty("totalJobs")]
        [Display("Total jobs")]
        public int TotalJobs { get; set; }

        [JsonProperty("completedJobs")]
        [Display("Completed jobs")]
        public int CompletedJobs { get; set; }

        [JsonProperty("sourceFiles")]
        [Display("Source files")]
        public IEnumerable<ProjectSourceFileStatus> SourceFiles { get; set; }
            = Enumerable.Empty<ProjectSourceFileStatus>();

        [JsonProperty("projectOriginator")]
        [Display("Project originator")]
        public string? ProjectOriginator { get; set; }

        [JsonProperty("attributes")]
        [Display("Attributes")]
        public IEnumerable<ProjectAttribute>? Attributes { get; set; }

        [JsonProperty("errorMessages")]
        [Display("Error messages")]
        public IEnumerable<string>? ErrorMessages { get; set; }

        [JsonProperty("deliverableFormats")]
        [Display("Deliverable formats")]
        public IEnumerable<string>? DeliverableFormats { get; set; }

        [JsonProperty("createdBy")]
        [Display("Created by")]
        public string? CreatedBy { get; set; }

        [JsonProperty("requesterName")]
        [Display("Requester name")]
        public string? RequesterName { get; set; }
    }

    public class ProjectSourceFileStatus
    {
        [JsonProperty("fileNumber")]
        [Display("File number")]
        public int FileNumber { get; set; }

        [JsonProperty("sourceLanguage")]
        [Display("Source language")]
        public string SourceLanguage { get; set; } = default!;

        [JsonProperty("fileName")]
        [Display("File name")]
        public string FileName { get; set; } = default!;

        [JsonProperty("translations")]
        [Display("Translations")]
        public IEnumerable<ProjectSourceFileTranslationStatus> Translations { get; set; }
            = Enumerable.Empty<ProjectSourceFileTranslationStatus>();

        [JsonProperty("sourceFilePath")]
        [Display("Source file path")]
        public string SourceFilePath { get; set; } = default!;
    }

    public class ProjectSourceFileTranslationStatus
    {
        [JsonProperty("targetLanguage")]
        [Display("Target language")]
        public string TargetLanguage { get; set; } = default!;

        [JsonProperty("isTranslated")]
        [Display("Is translated")]
        public bool IsTranslated { get; set; }

        [JsonProperty("translatedFilePath")]
        [Display("Translated file path")]
        public string? TranslatedFilePath { get; set; }
    }
}
