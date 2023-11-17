using System.Text;
using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Rbac.Api.Extensions;
using Rbac.Api.Infrastructure;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Rbac.Api.Services;

public class Data(ICosmosDbClientFactory factory, IAzureStorageClient storage) : IData
{
    private const string CollectionName = "users";

    public async Task<Dictionary<string, object>> GetAsync(Dictionary<string, object> item)
    {
        var query = $"select c.PartitionKey from c where c.upn='{item["upn"]}'";
        var result = await factory.GetClient(CollectionName).GetItemsAsync(query, new Dictionary<string, object>());
        return result[0];
    }

    public async Task UpdateAsync(Dictionary<string, object> item)
    {
        await factory.GetClient(CollectionName).UpsertItemAsync(item);
    }

    public async Task<string> DownloadAsync(string fileName)
    {
        var content = await storage.DownloadAsync(fileName);
        return content;
    }
    public async Task<string> QueryAsync()
    {
        var result = new StringBuilder();
        var credential = new ManagedIdentityCredential();
        var client = new LogsQueryClient(credential);
        var workspaceId = ContextManager.AppConfiguration.GetSection("AppSettings")["WorkspaceId"];
        var queryString = "AppRequests | limit 10";
        var timeRange = new QueryTimeRange(TimeSpan.FromDays(-1));

        Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(workspaceId, queryString, timeRange);

        foreach (var row in response.Value.Table.Rows)
        {
            result.AppendJoin(", ", row);
        }
        return result.ToString();
    }
}