namespace ReceiptWise.Core.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public const string AppName = "ReceiptWise";
    public const string DatabaseFilename = "receiptwise.db3";

    public static class Storage
    {
        public const string AttachmentsFolder = "attachments";
        public const string ThumbnailsFolder = "thumbnails";
        public const int MaxImageSizeKB = 5120; // 5MB
        public const int ThumbnailSize = 200;
    }

    public static class Azure
    {
        public const string DocumentIntelligenceEndpointKey = "AzureDocumentIntelligence:Endpoint";
        public const string DocumentIntelligenceApiKeyKey = "AzureDocumentIntelligence:ApiKey";
        public const string OpenAIEndpointKey = "AzureOpenAI:Endpoint";
        public const string OpenAIApiKeyKey = "AzureOpenAI:ApiKey";
        public const string OpenAIDeploymentKey = "AzureOpenAI:DeploymentName";
    }

    public static class Categories
    {
        public static readonly string[] DefaultCategories =
        {
            "Groceries",
            "Dining",
            "Transportation",
            "Shopping",
            "Healthcare",
            "Utilities",
            "Entertainment",
            "Travel",
            "Home & Garden",
            "Technology",
            "Services",
            "Other"
        };
    }
}