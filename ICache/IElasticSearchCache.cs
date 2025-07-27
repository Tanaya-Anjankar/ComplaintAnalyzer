using ComplaintAnalyzer;

namespace Complaint_Analyzer_using_ES.ICache
{
    public interface IElasticSearchCache
    {
        Task<List<Complaint>> ComplaintsAsync(string indexName, int page, int size);
        Task InvalidateComplaintCache(string indexName);
    }
}
