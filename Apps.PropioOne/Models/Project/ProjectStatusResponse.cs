using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.PropioOne.Models.Project
{
    public class ProjectStatusResponse
    {
        [JsonProperty("projectNumber")]
        [Display("Order ID")]
        public int ProjectNumber { get; set; }

        [JsonProperty("tmsReferenceNumber")]
        [Display("Tms reference number")]
        public string? TmsReferenceNumber { get; set; }

        [JsonProperty("customerNumber")]
        [Display("Customer number")]
        public int CustomerNumber { get; set; }

        [JsonProperty("projectType")]
        [Display("Order type")]
        public string ProjectType { get; set; } = default!;

        [JsonProperty("projectName")]
        [Display("Order name")]
        public string ProjectName { get; set; } = default!;

        [JsonProperty("projectStatus")]
        [Display("Order status")]
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
        [Display("Order originator")]
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

        [JsonProperty("quote")]
        [Display("Quote details")]
        public ProjectQuote? Quote { get; set; }
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

    public class ProjectQuote
    {
        [JsonProperty("platformQuoteNumber")]
        [Display("Quote number")]
        public string PlatformQuoteNumber { get; set; } = default!;

        [JsonProperty("quoteStatus")]
        [Display("Quote status")]
        public string? QuoteStatus { get; set; }

        [JsonProperty("clientInvoiceId")]
        [Display("Client invoice ID")]
        public int? ClientInvoiceId { get; set; }

        [JsonProperty("currency")]
        [Display("Currency")]
        public string? Currency { get; set; }

        [JsonProperty("totalAmount")]
        [Display("Total amount")]
        public double? TotalAmount { get; set; }

        [JsonProperty("totalNetAmount")]
        [Display("Total net amount")]
        public double? TotalNetAmount { get; set; }

        [JsonProperty("vat")]
        [Display("VAT")]
        public double? Vat { get; set; }

        [JsonProperty("finalTotalAmount")]
        [Display("Final total amount")]
        public double? FinalTotalAmount { get; set; }

        [JsonProperty("rejectReason")]
        [Display("Reject reason")]
        public string? RejectReason { get; set; }

        [JsonProperty("createdDate")]
        [Display("Created date")]
        public DateTime CreatedDate { get; set; }

        [JsonProperty("quoteLineItems")]
        [Display("Quote line items")]
        public IEnumerable<ProjectQuoteLineItem>? QuoteLineItems { get; set; }
    }

    public class ProjectQuoteLineItem
    {
        [JsonProperty("lineItemIndex")]
        [Display("Line item index")]
        public int? LineItemIndex { get; set; }

        [JsonProperty("lineItemHeader")]
        [Display("Line item header")]
        public string? LineItemHeader { get; set; }

        [JsonProperty("lineItemWorkType")]
        [Display("Line item work type")]
        public string? LineItemWorkType { get; set; }

        [JsonProperty("unitPrice")]
        [Display("Unit price")]
        public double? UnitPrice { get; set; }

        [JsonProperty("netTotal")]
        [Display("Net total")]
        public double? NetTotal { get; set; }

        [JsonProperty("quantity")]
        [Display("Quantity")]
        public double? Quantity { get; set; }
    }
}
