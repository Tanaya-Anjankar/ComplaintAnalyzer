using Complaint_Analyzer_using_ES.Helpers;
using Complaint_Analyzer_using_ES.ICache;
using Nest;

namespace ComplaintAnalyzer
{
    public class ElasticSearchService : IElasticSearchService
    {
        private readonly IElasticClient _client;
        private const string IndexName = "complaints";
        public ElasticSearchService(IElasticClient client)
        {
            _client = client;
        }

        // Create the index.
        public async Task<string> CreateIndexAsync(string indexName)
        {
            ExistsResponse existsResponse = await _client.Indices.ExistsAsync(indexName);

            if (existsResponse.Exists)
            {
                throw new ConflictException($"Index '{indexName}' already exists.");
            }

            CreateIndexResponse createIndexResponse = await _client.Indices.CreateAsync(indexName, c => c
                .Map<Complaint>(m => m.AutoMap())
            );

            if (!createIndexResponse.IsValid)
            {
                throw new ApplicationException($"Failed to create index: {createIndexResponse.ServerError?.Error?.Reason}");
            }

            return $"Index '{indexName}' created successfully.";
        }

        // Get All Index Name
        public async Task<List<string>> GetAllIndexNamesAsync()
        {
            var response = await _client.Cat.IndicesAsync(); // calls _cat/indices API

            if (!response.IsValid)
                throw new ApplicationException("Failed to retrieve indices.");

            return response.Records
                .Select(record => record.Index)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        // Add Complaint to the index.
        public async Task<string> AddComplaintsAsync(string indexName, List<Complaint> complaints)
        {
            try
            {
                ExistsResponse existsResponse = await _client.Indices.ExistsAsync(indexName);

                if (!existsResponse.Exists)
                {
                    throw new InvalidOperationException($"Index '{indexName}' does not exist.");
                }

                BulkDescriptor bulkDescriptor = new BulkDescriptor();

                foreach (Complaint complaint in complaints)
                {
                    bulkDescriptor.Index<Complaint>(op => op
                        .Index(indexName)
                        .Document(complaint)
                        .Id(complaint.Id)
                    );
                }

                // Execute the bulk request
                BulkResponse response = await _client.BulkAsync(bulkDescriptor);

                if (response.Errors)
                {
                    throw new Exception("Some or all documents failed to index.");
                }
                return "Bulk complaints indexed successfully.";
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to add complaints to index '{indexName}': {ex.Message}", ex);
            }
        }

        // Search complaint in the index based on keyword.
        public async Task<List<Complaint>> SearchAsync(string indexName, string keyword, string? status = null, string? category = null)
        {
            var mustQueries = new List<Func<QueryContainerDescriptor<Complaint>, QueryContainer>>
            {
                m => m.Match(mt => mt.Field(f => f.Description).Query(keyword))
            };

            if (!string.IsNullOrEmpty(status))
                mustQueries.Add(m => m.Term(t => t.Field(f => f.Status).Value(status)));

            if (!string.IsNullOrEmpty(category))
                mustQueries.Add(m => m.Term(t => t.Field(f => f.Category).Value(category)));

            var response = await _client.SearchAsync<Complaint>(s => s
                .Index(indexName)
                .Query(q => q.Bool(b => b.Must(mustQueries)))
                .Size(100)
            );

            if (!response.IsValid)
                return new List<Complaint>(); 

            return response.Documents.ToList();
        }

        // Get All Complaints in the index.
        public async Task<List<Complaint>> GetAllComplaintsAsync(string indexName, int pageNumber, int pageSize)
        {
            var exists = await _client.Indices.ExistsAsync(indexName);
            if (!exists.Exists)
            {
                throw new InvalidOperationException($"Index '{indexName}' does not exist.");
            }

            var from = (pageNumber - 1) * pageSize;

            var response = await _client.SearchAsync<Complaint>(s => s
                .Index(indexName)
                .Query(q => q.MatchAll())
                .From(from)
                .Size(pageSize)
            );

            return response.Documents.ToList();
        }

        // Samrt seach based on wildcard and Fuzziness.
        public async Task<List<ComplaintSearchResult>> SmartSearchAsync(string indexName, string keyword, string? status, string? category, int pageNumber, int pageSize)
        {
            var exists = await _client.Indices.ExistsAsync(indexName);
            if (!exists.Exists)
            {
                throw new InvalidOperationException($"Index '{indexName}' does not exist.");
            }
            var shouldQueries = new List<Func<QueryContainerDescriptor<Complaint>, QueryContainer>>
            {
                s => s.Match(m => m
                    .Field(f => f.Description)
                    .Query(keyword)
                    .Fuzziness(Fuzziness.Auto)),

                s => s.Wildcard(w => w
                    .Field(f => f.Description.Suffix("keyword"))
                    .Value($"*{keyword.ToLower()}*"))
            };

            var mustQueries = new List<Func<QueryContainerDescriptor<Complaint>, QueryContainer>>
            {
                s => s.Bool(b => b.Should(shouldQueries).MinimumShouldMatch(1))
            };

            if (!string.IsNullOrEmpty(status))
                mustQueries.Add(q => q.Term(t => t.Field(f => f.Status).Value(status)));

            if (!string.IsNullOrEmpty(category))
                mustQueries.Add(q => q.Term(t => t.Field(f => f.Category).Value(category)));

            var response = await _client.SearchAsync<Complaint>(s => s
                .Index(indexName)
                .From((pageNumber - 1) * pageSize)
                .Size(pageSize)
                .Query(q => q.Bool(b => b.Must(mustQueries)))
                .Highlight(h => h
                    .Fields(f => f
                        .Field(ff => ff.Description)
                        .PreTags("<b>").PostTags("</b>")
                    )
                )
            );

            return response.Hits.Select(hit => new ComplaintSearchResult
            {
                Id = hit.Source.Id,
                Description = hit.Source.Description,
                Status = hit.Source.Status,
                Category = hit.Source.Category,
                HighlightedDescription = hit.Highlight?.Values.FirstOrDefault()?.FirstOrDefault() ?? hit.Source.Description
            }).ToList();
        }


    }
}
