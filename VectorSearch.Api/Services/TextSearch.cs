namespace VectorSearch.Api.Services;
public class TextSearch : BaseSearch, ISearch
{
    private readonly OpenAIClient _openAiClient;

    public TextSearch(ILogger logger) : base(logger)
    {
        var credential = new AzureKeyCredential(AppSettings.AzureOpenAiKey);
        var searchCredential = new AzureKeyCredential(AppSettings.AzureSearchAdminKey);

        _openAiClient = new OpenAIClient(new Uri(AppSettings.AzureOpenAiEndpoint), credential);
        _indexClient = new SearchIndexClient(new Uri(AppSettings.AzureSearchServiceEndpoint), searchCredential);
        _searchClient = _indexClient.GetSearchClient(AppSettings.AzureSearchIndexName);
    }

    public async Task CreateIndexAsync()
    {
        _logger.LogTrace("Create Index");
        await _indexClient.CreateOrUpdateIndexAsync(GetSampleIndex(AppSettings.AzureSearchIndexName)).ConfigureAwait(false); ;
        var path = Directory.GetCurrentDirectory() + "/data/text-sample.json";
        // Read input documents and generate embeddings  
        var inputJson = await File.ReadAllTextAsync(path);
        var inputDocuments = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(inputJson) ?? new List<Dictionary<string, object>>();

        var sampleDocuments = await GetSampleDocumentsAsync(inputDocuments);
        await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(sampleDocuments));
    }

    public async Task<Dictionary<string, object>> SearchIndexAsync(string inputQuery, string filter, int searchType)
    {
        var response = searchType switch
        {
            1 => await SingleVectorSearch(inputQuery),
            2 => await SingleVectorSearchWithFilter(inputQuery, filter),
            3 => await SimpleHybridSearch(inputQuery),
            4 => await SemanticHybridSearch(inputQuery),
            _ => throw new ArgumentOutOfRangeException("Invalid search.", (Exception?)null)
        };
        return response;
    }

    #region Private Members

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddings(string text)
    {
        var response = await _openAiClient.GetEmbeddingsAsync(new EmbeddingsOptions(AppSettings.AzureOpenAiDeploymentModel, new List<string> { text }));
        return response.Value.Data[0].Embedding;
    }

    private async Task<Dictionary<string, object>> SingleVectorSearch(string query, int k = 3)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Generate the embedding for the query  
        var queryEmbeddings = await GenerateEmbeddings(query);

        // Perform the vector similarity search  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new RawVectorQuery { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = k, Fields = { "contentVector" } } },
            Size = k,
            Select = { "title", "content", "category" }
        };

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions);

        var count = 0;
        await foreach (var result in response.GetResultsAsync())
        {
            count++;
            var item = new Dictionary<string, object>
            {
                { "Title", result.Document["title"] },
                { "Score", result.Score },
                { "Content", result.Document["content"] },
                { "Category", result.Document["category"] }
            };

            items.Add(item);
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    internal async Task<Dictionary<string, object>> SingleVectorSearchWithFilter(string query, string filter)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Generate the embedding for the query  
        var queryEmbeddings = await GenerateEmbeddings(query);

        // Perform the vector similarity search  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new RawVectorQuery { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = 3, Fields = { "contentVector" } } },
            Filter = filter,
            Select = { "title", "content", "category" }
        };

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions);

        var count = 0;
        await foreach (var result in response.GetResultsAsync())
        {
            count++;
            var item = new Dictionary<string, object>
            {
                { "Title", result.Document["title"] },
                { "Score", result.Score },
                { "Content", result.Document["content"] },
                { "Category", result.Document["category"] }
            };

            items.Add(item);
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    internal async Task<Dictionary<string, object>> SimpleHybridSearch(string query)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Generate the embedding for the query  
        var queryEmbeddings = await GenerateEmbeddings(query);

        // Perform the vector similarity search  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new RawVectorQuery { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = 3, Fields = { "contentVector" } } },
            Size = 10,
            Select = { "title", "content", "category" }
        };

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);

        var count = 0;
        await foreach (var result in response.GetResultsAsync())
        {
            count++;
            var item = new Dictionary<string, object>
            {
                { "Title", result.Document["title"] },
                { "Score", result.Score },
                { "Content", result.Document["content"] },
                { "Category", result.Document["category"] }
            };

            items.Add(item);
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    internal async Task<Dictionary<string, object>> SemanticHybridSearch(string query)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        try
        {
            // Generate the embedding for the query  
            var queryEmbeddings = await GenerateEmbeddings(query);

            // Perform the vector similarity search  
            var searchOptions = new SearchOptions
            {
                VectorQueries = { new RawVectorQuery { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = 3, Fields = { "contentVector" } } },
                Size = 3,
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
                SemanticConfigurationName = AppSettings.SemanticSearchConfigName,
                QueryCaption = QueryCaptionType.Extractive,
                QueryAnswer = QueryAnswerType.Extractive,
                QueryCaptionHighlightEnabled = true,
                Select = { "title", "content", "category" }
            };

            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);

            var count = 0;

            var ans = new List<Dictionary<string, object>>();
            foreach (var result in response.Answers)
            {
                var item = new Dictionary<string, object>
                {
                    { "AnswerHighlights", result.Highlights },
                    { "AnswerText", result.Text }
                };
                ans.Add(item);
            }

            documents.Add("QueryAnswer", ans);

            await foreach (var result in response.GetResultsAsync())
            {
                count++;
                var item = new Dictionary<string, object>
                {
                    { "Title", result.Document["title"] },
                    { "RerankerScore", result.RerankerScore },
                    { "Score", result.Score },
                    { "Content", result.Document["content"] },
                    { "Category", result.Document["category"] }
                };

                if (result.Captions != null)
                {
                    var caption = result.Captions.FirstOrDefault();
                    if (caption != null)
                    {
                        item.Add("CaptionHighlights", caption.Highlights);
                        item.Add("CaptionText", caption.Text);
                    }
                }
                items.Add(item);
            }

            documents.Add("Data", items);
            documents.Add("Total", count);
        }
        catch (NullReferenceException)
        {
            documents.Add("Total", 0);
        }

        return documents;
    }

    internal static SearchIndex GetSampleIndex(string name)
    {
        const string vectorSearchProfile = "my-vector-profile";
        const string vectorSearchHnswConfig = "my-hnsw-vector-config";

        SearchIndex searchIndex = new(name)
        {
            VectorSearch = new Azure.Search.Documents.Indexes.Models.VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchProfile, vectorSearchHnswConfig)
                },
                Algorithms =
                {
                    new HnswVectorSearchAlgorithmConfiguration(vectorSearchHnswConfig)
                }
            },
            SemanticSettings = new SemanticSettings
            {

                Configurations =
                    {
                       new SemanticConfiguration(AppSettings.SemanticSearchConfigName, new PrioritizedFields
                       {
                           TitleField = new SemanticField { FieldName = "title" },
                           ContentFields =
                           {
                               new SemanticField { FieldName = "content" }
                           },
                           KeywordFields =
                           {
                               new SemanticField { FieldName = "category" }
                           }
                       })
                }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("title") { IsFilterable = true, IsSortable = true },
                new SearchableField("content") { IsFilterable = true },
                new SearchField("titleVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = AppSettings.ModelDimensions,
                    VectorSearchProfile = vectorSearchProfile
                },
                new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = AppSettings.ModelDimensions,
                    VectorSearchProfile = vectorSearchProfile
                },
                new SearchableField("category") { IsFilterable = true, IsSortable = true, IsFacetable = true }
            }
        };
        return searchIndex;
    }

    internal async Task<List<SearchDocument>> GetSampleDocumentsAsync(List<Dictionary<string, object>> inputDocuments)
    {
        var sampleDocuments = new List<SearchDocument>();

        foreach (var document in inputDocuments)
        {
            var title = document["title"].ToString() ?? string.Empty;
            var content = document["content"].ToString() ?? string.Empty;
            var titleEmbeddings = (await GenerateEmbeddings(title)).ToArray();
            var contentEmbeddings = (await GenerateEmbeddings(content)).ToArray();

            document["titleVector"] = titleEmbeddings;
            document["contentVector"] = contentEmbeddings;
            sampleDocuments.Add(new SearchDocument(document));
        }

        return sampleDocuments;
    }

    #endregion
}