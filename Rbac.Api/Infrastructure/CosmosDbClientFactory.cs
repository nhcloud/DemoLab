using Microsoft.Azure.Cosmos;

namespace Rbac.Api.Infrastructure;

public class CosmosDbClientFactory(string? databaseName, List<string>? collectionNames, CosmosClient cosmosClient) : ICosmosDbClientFactory
{
    private readonly List<string> _collectionNames = collectionNames ?? throw new ArgumentNullException(nameof(collectionNames));
    private readonly CosmosClient _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
    private readonly string _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));

    public ICosmosDbClient GetClient(string collectionName)
    {
        if (!_collectionNames.Contains(collectionName))
        {
            throw new ArgumentException($"Unable to find collection: {collectionName}");
        }

        return new CosmosDbClient(_cosmosClient.GetDatabase(_databaseName), collectionName);
    }
}