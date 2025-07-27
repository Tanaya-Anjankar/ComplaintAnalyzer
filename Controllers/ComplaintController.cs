using Complaint_Analyzer_using_ES.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ComplaintAnalyzer
{
    public class ComplaintController : Controller
    {
        private readonly IElasticSearchService _elasticService;

        public ComplaintController(IElasticSearchService elasticService)
        {
            _elasticService = elasticService;
        }
        /// <summary>
        /// Creates a new Elasticsearch index with the given name.
        /// </summary>
        /// <param name="indexName">Name of the index to be created</param>
        [HttpPost("create-index/{indexName}")]
        public async Task<IActionResult> CreateIndex(string indexName)
        {
            try
            {
                var result = await _elasticService.CreateIndexAsync(indexName.ToLower());
                return Ok(result);
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }


        /// <summary>
        /// Adds multiple complaints to the specified index.
        /// </summary>
        /// <param name="indexName">The index name where documents will be added.</param>
        /// <param name="complaints">List of complaints to add.</param>
        /// <returns>Success or error result.</returns>
        [HttpPost("add-bulk/{indexName}")]
        public async Task<IActionResult> AddComplaints(string indexName, [FromBody] List<Complaint> complaints)
        {
            try
            {
                var result = await _elasticService.AddComplaintsAsync(indexName, complaints);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Index does not exist
                return NotFound(ex.Message);
            }
            catch (ApplicationException ex)
            {
                // Custom application-level exception
                return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                // Unhandled errors
                return StatusCode(500, $"Unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetches paginated complaints from the specified index.
        /// </summary>
        /// <param name="indexName">Index to query.</param>
        /// <param name="pageNumber">Page number (1-based).</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <returns>Paged list of complaints.</returns>
        [HttpGet("all/{indexName}")]
        public async Task<IActionResult> GetAllComplaints(string indexName, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = await _elasticService.GetAllComplaintsAsync(indexName, pageNumber, pageSize);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong.", details = ex.Message });
            }
        }



        /// <summary>
        /// Searches complaints in a specified index using a keyword with optional filters.
        /// </summary>
        /// <param name="indexName">The name of the index to search.</param>
        /// <param name="keyword">The keyword to search in the complaint description.</param>
        /// <param name="status">Optional status filter (e.g., "open", "closed").</param>
        /// <param name="category">Optional category filter (e.g., "delivery", "refund").</param>
        /// <returns>List of matched complaints.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery][Required] string keyword, [FromQuery][Required] string indexname, [FromQuery] string? status, [FromQuery] string? category)
        {
            var result = await _elasticService.SearchAsync(indexname, keyword, status, category);
            return Ok(result);
        }

        /// <summary>
        /// Smart full-text search API for complaints with filters, pagination, and highlight support.
        /// </summary>
        /// <param name="indexName">The name of the Elasticsearch index to search in.</param>
        /// <param name="keyword">The keyword to search in the complaint description.</param>
        /// <param name="status">Optional status filter (e.g., Open, Closed).</param>
        /// <param name="category">Optional category filter (e.g., Payment, Delivery).</param>
        /// <param name="pageNumber">Page number for pagination (default is 1).</param>
        /// <param name="pageSize">Page size for pagination (default is 10).</param>
        /// <returns>List of complaints matching the search criteria with highlighted descriptions.</returns>
        [HttpGet("advance-search")]
        [ProducesResponseType(typeof(List<ComplaintSearchResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchComplaints([FromQuery][Required] string indexName, [FromQuery][Required] string keyword,
    [FromQuery] string? status = null, [FromQuery] string? category = null,
    [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                List<ComplaintSearchResult> results = await _elasticService.SmartSearchAsync(indexName, keyword, status, category, pageNumber, pageSize);
                return Ok(results);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong.", details = ex.Message });
            }
            
        }

    }
}
