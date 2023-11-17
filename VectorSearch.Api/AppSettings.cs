namespace VectorSearch.Api;

public class AppSettings
{
    public const int ModelDimensions = 1536;
    public const string SemanticSearchConfigName = "my-semantic-config";

    static AppSettings()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json").Build();

        AzureSearchServiceEndpoint = configuration["AZURE_SEARCH_SERVICE_ENDPOINT"] ?? string.Empty;
        AzureSearchIndexName = configuration["AZURE_SEARCH_INDEX_NAME"] ?? string.Empty;
        AzureSearchAdminKey = configuration["AZURE_SEARCH_ADMIN_KEY"] ?? string.Empty;
        AzureOpenAiKey = configuration["AZURE_OPENAI_API_KEY"] ?? string.Empty;
        AzureOpenAiEndpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? string.Empty;
        AzureOpenAiDeploymentModel = configuration["AZURE_OPENAI_EMBEDDING_DEPLOYED_MODEL"] ?? string.Empty;
        BlobConnectionString = configuration["AZURE_BLOB_CONNECTION_STRING"] ?? string.Empty;
        BlobContainerName = configuration["AZURE_BLOB_CONTAINER_NAME"] ?? string.Empty;
        SampleToRun= configuration["SAMPLE_TO_RUN"] ?? "TextSearch";
    }

    public static string SampleToRun { get; private set; }
    public static string AzureSearchServiceEndpoint { get; private set; }
    public static string AzureSearchIndexName { get; private set; }
    public static string AzureSearchAdminKey { get; private set; }
    public static string AzureOpenAiEndpoint { get; private set; }
    public static string AzureOpenAiKey { get; private set; }
    public static string AzureOpenAiDeploymentModel { get; private set; }
    public static string BlobConnectionString { get; private set; }
    public static string BlobContainerName { get; private set; }
}