using Complaint_Analyzer_using_ES.ICache;
using Complaint_Analyzer_using_ES.IServices;
using ComplaintAnalyzer;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Complaint_Analyzer_using_ES.Cache
{
    public class ElasticSearchCache : IElasticSearchCache
    {
        private readonly IElasticSearchService _elasticService;
        private readonly IRedisCacheService redisCacheService;

        public ElasticSearchCache(IElasticSearchService elasticService, IRedisCacheService redis)
        {
            _elasticService = elasticService;
            redisCacheService = redis;
        }

        // return data from cache ifnot available in cache fetch from elastic search and insert in cache.
        public async Task<List<Complaint>> ComplaintsAsync(string indexName, int page, int size)
        {
            string cacheKey = $"complaints:{indexName}:{page}:{size}";

            List<Complaint> Complaints = await redisCacheService.GetAsync<List<Complaint>>(cacheKey);
            if(Complaints == null) {

                Complaints = await _elasticService.GetAllComplaintsAsync(indexName, page, size);
                await redisCacheService.SetAsync(cacheKey, Complaints, TimeSpan.FromMinutes(10));
            }                
            return Complaints;
        }

        // Remove complaint data from cache for given index name.
        public async Task InvalidateComplaintCache(string indexName)
        {
            for (int page = 1; page <= 3; page++) 
            {
                await redisCacheService.RemoveAsync($"complaints:{indexName}:{page}:10");
            }

        }


    }
}
