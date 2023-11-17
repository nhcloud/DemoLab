namespace VectorSearch.Api.Services;

public interface ISearch
{
    Task CreateIndexAsync();
    Task<Dictionary<string, object>> SearchIndexAsync(string inputQuery, string filter, int searchType);
}