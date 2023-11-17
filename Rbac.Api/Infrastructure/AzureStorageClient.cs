using Azure.Identity;
using Azure.Storage.Blobs;

namespace Rbac.Api.Infrastructure;

public class AzureStorageClient(string connectionString, string containerName) : IAzureStorageClient
{
    private readonly BlobContainerClient _blobContainerClient = connectionString.StartsWith("https://") ? new BlobServiceClient(new Uri(connectionString + ".blob.core.windows.net"), new DefaultAzureCredential()).GetBlobContainerClient(containerName) : new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);

    public async Task<string> DownloadAsync(string fileName)
    {
        var content = await _blobContainerClient.GetBlobClient(fileName).DownloadContentAsync();
        return content.Value.Content.ToString();
    }
}