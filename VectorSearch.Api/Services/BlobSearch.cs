namespace VectorSearch.Api.Services;

public class BlobSearch : BaseSearch, ISearch
{
    private readonly SearchIndexerClient _indexerClient;

    public BlobSearch(ILogger logger) : base(logger)
    {
        var searchCredential = new AzureKeyCredential(AppSettings.AzureSearchAdminKey);

        _indexClient = new SearchIndexClient(new Uri(AppSettings.AzureSearchServiceEndpoint), searchCredential);
        _indexerClient = new SearchIndexerClient(new Uri(AppSettings.AzureSearchServiceEndpoint), searchCredential);
        _searchClient = _indexClient.GetSearchClient(AppSettings.AzureSearchIndexName);
    }

    public async Task CreateIndexAsync()
    {

        // Create an Index  
        _logger.LogTrace("Creating/Updating the index...");
        var index = GetSampleIndex(AppSettings.AzureSearchIndexName);
        await _indexClient.CreateOrUpdateIndexAsync(index).ConfigureAwait(false);
        _logger.LogTrace("Index Created/Updated!");

        // Create a Data Source Connection  
        _logger.LogTrace("Creating/Updating the data source connection...");
        var dataSource = new SearchIndexerDataSourceConnection(
            $"{AppSettings.AzureSearchIndexName}-blob",
            SearchIndexerDataSourceType.AzureBlob,
            AppSettings.BlobConnectionString,
            new SearchIndexerDataContainer(AppSettings.BlobContainerName));
        await _indexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSource);
        _logger.LogTrace("Data Source Created/Updated!");

        // Create a Skillset  
        _logger.LogTrace("Creating/Updating the skillset...");
        var skillSet = new SearchIndexerSkillset($"{AppSettings.AzureSearchIndexName}-skillset", new List<SearchIndexerSkill>
                {  
                    // Add required skills here    
                    new SplitSkill(
                        new List<InputFieldMappingEntry>
                        {
                            new("text") { Source = "/document/content" }
                        },
                        new List<OutputFieldMappingEntry>
                        {
                            new("textItems") { TargetName = "pages" }
                        })
                    {
                        Context = "/document",
                        TextSplitMode = TextSplitMode.Pages,
                        MaximumPageLength = 500,
                        PageOverlapLength = 100
                    },
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry>
                        {
                            new("text") { Source = "/document/pages/*" }
                        },
                        new List<OutputFieldMappingEntry>
                        {
                            new("embedding") { TargetName = "vector" }
                        }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(AppSettings.AzureOpenAiEndpoint),
                        ApiKey = AppSettings .AzureOpenAiKey,
                        DeploymentId = "text-embedding-ada-002"
                    }
                });
        await _indexerClient.CreateOrUpdateSkillsetAsync(skillSet).ConfigureAwait(false);
        _logger.LogTrace("Skillset Created/Updated!");

        // Create an Indexer  
        _logger.LogTrace("Creating/Updating the indexer...");
        var indexer = new SearchIndexer($"{AppSettings.AzureSearchIndexName}-indexer", dataSource.Name, AppSettings.AzureSearchIndexName)
        {
            Description = "Indexer to chunk documents, generate embeddings, and add to the index",
            Schedule = new IndexingSchedule(TimeSpan.FromDays(1))
            {
                StartTime = DateTimeOffset.Now
            },
            Parameters = new IndexingParameters
            {
                BatchSize = 1,
                MaxFailedItems = 0,
                MaxFailedItemsPerBatch = 0
            },
            SkillsetName = skillSet.Name
        };
        await _indexerClient.CreateOrUpdateIndexerAsync(indexer).ConfigureAwait(false);
        _logger.LogTrace("Indexer Created/Updated!");

        // Run Indexer  
        _logger.LogTrace("Running the indexer...");
        await _indexerClient.RunIndexerAsync(indexer.Name).ConfigureAwait(false);
        _logger.LogTrace("Indexer is Running!");
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

    private static SearchIndex GetSampleIndex(string name)
    {
        SearchIndex searchIndex = new(name)
        {
            VectorSearch = new Azure.Search.Documents.Indexes.Models.VectorSearch
            {
                Profiles =
            {
                new VectorSearchProfile(VectorConstants.vectorSearchHnswProfile, VectorConstants.vectorSearchHnswConfig)
                {
                    Vectorizer = VectorConstants.vectorSearchVectorizer
                },
                new VectorSearchProfile(VectorConstants.vectorSearchExhasutiveKnnProfile, VectorConstants.vectorSearchExhaustiveKnnConfig)
            },
                Algorithms =
            {
                new HnswVectorSearchAlgorithmConfiguration(VectorConstants.vectorSearchHnswConfig),
                new ExhaustiveKnnVectorSearchAlgorithmConfiguration(VectorConstants.vectorSearchExhaustiveKnnConfig)
            },
                Vectorizers =
            {
                new AzureOpenAIVectorizer(VectorConstants.vectorSearchVectorizer)
                {
                    AzureOpenAIParameters = new AzureOpenAIParameters
                    {
                        ResourceUri = new Uri(AppSettings .AzureOpenAiEndpoint ),
                        ApiKey = AppSettings.AzureOpenAiKey,
                        DeploymentId = AppSettings .AzureOpenAiDeploymentModel
                    }
                }
            }
            },
            SemanticSettings = new SemanticSettings
            {
                Configurations =
            {
                new SemanticConfiguration(AppSettings .SemanticSearchConfigName, new PrioritizedFields
                {
                    TitleField = new SemanticField { FieldName = "title" },
                    ContentFields =
                    {
                        new SemanticField { FieldName = "chunk" }
                    }
                })
            }
            },
            Fields =
        {
            new SearchableField("parent_id") { IsFilterable = true, IsSortable = true, IsFacetable = true },
            new SearchableField("chunk_id") { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true, AnalyzerName = LexicalAnalyzerName.Keyword },
            new SearchableField("title"),
            new SearchableField("chunk"),
            new SearchField("vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = AppSettings .ModelDimensions,
                VectorSearchProfile = VectorConstants.vectorSearchHnswProfile
            },
            new SearchableField("category") { IsFilterable = true, IsSortable = true, IsFacetable = true }
        }
        };

        return searchIndex;
    }

    private async Task<Dictionary<string, object>> SingleVectorSearch(string query, int k = 3)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Perform the vector similarity search  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new VectorizableTextQuery
            {
            Text = query,
            KNearestNeighborsCount = k,
            Fields = { "vector" }
        } },
            Size = k,
            Select = { "title", "chunk_id", "chunk" }
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
                { "Content", result.Document["chunk"] }
            };
            items.Add(item);
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    private async Task<Dictionary<string, object>> SingleVectorSearchWithFilter(string query, string filter, int k = 3)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Perform the vector similarity search with filter  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new VectorizableTextQuery
            {
            Text = query,
            KNearestNeighborsCount = k,
            Fields = { "vector" }
        } },
            Filter = filter,
            Size = k,
            Select = { "title", "chunk", "category" }
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
                { "Content", result.Document["chunk"] }
            };
            items.Add(item);
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    private async Task<Dictionary<string, object>> SimpleHybridSearch(string query, int k = 3)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Perform the simple hybrid search  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new VectorizableTextQuery
            {
            Text = query,
            KNearestNeighborsCount = k,
            Fields = { "vector" }
        } },
            Size = k,
            Select = { "title", "chunk", "category" }
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
                { "Content", result.Document["chunk"] }
            };
            items.Add(item);
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    private async Task<Dictionary<string, object>> SemanticHybridSearch(string query, int k = 3)
    {
        var documents = new Dictionary<string, object>();
        var items = new List<Dictionary<string, object>>();
        // Perform the semantic hybrid search  
        var searchOptions = new SearchOptions
        {
            VectorQueries = { new VectorizableTextQuery
                {
                Text = query,
                KNearestNeighborsCount = k,
                Fields = { "vector" }
                }
            },
            QueryType = SearchQueryType.Semantic,
            QueryLanguage = QueryLanguage.EnUs,
            SemanticConfigurationName = "my-semantic-config",
            QueryCaption = QueryCaptionType.Extractive,
            QueryAnswer = QueryAnswerType.Extractive,
            Size = k,
            Select = { "title", "chunk", "category" }
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

            var item = new Dictionary<string, object>
            {
                { "Title", result.Document["title"] },
                { "RerankerScore", result.RerankerScore },
                { "Score", result.Score },
                { "Content", result.Document["chunk"] }
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
        }
        documents.Add("Data", items);
        documents.Add("Total", count);
        return documents;
    }

    #endregion
}