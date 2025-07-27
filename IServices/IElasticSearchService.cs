namespace ComplaintAnalyzer
{
    public interface IElasticSearchService
    {
        Task<string> CreateIndexAsync(string indexName);
        Task<string> AddComplaintsAsync(string indexName, List<Complaint> complaints);
        Task<List<Complaint>> SearchAsync(string keyword, string indexName, string? status, string? category);
        Task<List<Complaint>> GetAllComplaintsAsync(string indexName, int pageNumber, int pageSize);
        Task<List<ComplaintSearchResult>> SmartSearchAsync(string indexName, string keyword, string? status, string? category, int pageNumber, int pageSize);
        Task<List<string>> GetAllIndexNamesAsync();


    }
}
