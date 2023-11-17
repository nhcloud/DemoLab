namespace VectorSearch.Api.Services;
public abstract class BaseSearch(ILogger logger)
{
    protected SearchIndexClient? _indexClient;
    protected ILogger? _logger = logger;
    protected SearchClient? _searchClient;
}