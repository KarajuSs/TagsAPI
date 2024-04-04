using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO.Compression;
using TagsAPI.Data;
using TagsAPI.Model;
using System.Linq.Expressions;

[ApiController]
[Route("[controller]")]
public class TagsController : ControllerBase
{
    private readonly ILogger<TagsController> _logger;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public TagsController(ILogger<TagsController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = null;
    }

    public TagsController(ILogger<TagsController> logger, AppDbContext context, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    // Metoda do pobierania tagów z API StackOverflow
    [HttpGet("fetch")]
    public async Task<IActionResult> FetchTags()
    {
        try
        {
            _logger.LogInformation("Started fetching tags from StackOverflow API.");

            var allTags = new List<Tag>();
            var pageNumber = 1;
            var pageSize = 100;

            while (allTags.Count < 1000)
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync($"https://api.stackexchange.com/2.3/tags?page={pageNumber}&pagesize={pageSize}&order=desc&sort=popular&site=stackoverflow");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch tags. Status code: {(int)response.StatusCode}");
                    return StatusCode((int)response.StatusCode);
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    if (response.Content.Headers.ContentEncoding.Any(x => x.ToLower() == "gzip"))
                    {
                        using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                        using (var reader = new StreamReader(gzipStream))
                        {
                            var content = await reader.ReadToEndAsync();
                            var tagsResponse = JsonConvert.DeserializeObject<TagsResponse>(content);
                            var tags = tagsResponse.Items;

                            allTags.AddRange(tags);

                            if (tags.Count < pageSize)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var content = await reader.ReadToEndAsync();
                            var tagsResponse = JsonConvert.DeserializeObject<TagsResponse>(content);
                            var tags = tagsResponse.Items;

                            allTags.AddRange(tags);

                            if (tags.Count < pageSize)
                            {
                                break;
                            }
                        }
                    }
                }

                pageNumber++;
            }

            await _context.Tags.AddRangeAsync(allTags);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Fetched {allTags.Count} tags from StackOverflow API successfully.");

            return Ok("Tags fetched successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while fetching tags: {ex.Message}");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    // Metoda do obliczania procentowego udziału tagów
    [HttpGet("percentage")]
    public async Task<IActionResult> CalculatePercentage()
    {
        try
        {
            var totalTagsCount = await _context.Tags.SumAsync(t => t.Count);
            var tags = await _context.Tags.ToListAsync();

            var percentageTags = tags.Select(t => new { Name = t.Name, Percentage = Math.Round((double)t.Count / totalTagsCount * 100, 2) });

            return Ok(percentageTags);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    // Metoda do udostępniania tagów poprzez stronicowane API
    [HttpGet]
    public async Task<IActionResult> GetTags(int pageNumber = 1, int pageSize = 10, string sortBy = "name", string sortOrder = "asc")
    {
        try
        {
            var query = _context.Tags.AsQueryable();

            // Logowanie informacji o żądaniu
            _logger.LogInformation($"Received request to get tags. PageNumber: {pageNumber}, PageSize: {pageSize}, SortBy: {sortBy}, SortOrder: {sortOrder}");

            // Sortowanie
            var parameter = Expression.Parameter(typeof(Tag), "x");
            var property = Expression.Property(parameter, sortBy);
            var lambda = Expression.Lambda(property, parameter);

            var orderByMethod = sortOrder.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
            var orderByExpression = Expression.Call(typeof(Queryable), orderByMethod, new Type[] { typeof(Tag), property.Type },
                                                     query.Expression, Expression.Quote(lambda));
            query = query.Provider.CreateQuery<Tag>(orderByExpression);

            // Stronicowanie
            var tags = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(tags);
        }
        catch (Exception ex)
        {
            // Logowanie błędu
            _logger.LogError($"An error occurred while processing the request: {ex}");

            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    // Metoda do wymuszenia ponownego pobrania tagów z API StackOverflow
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTags()
    {
        try
        {
            // Usunięcie istniejących tagów z bazy danych
            _context.Tags.RemoveRange(_context.Tags);
            await _context.SaveChangesAsync();

            // Ponowne pobranie tagów z API StackOverflow
            return await FetchTags();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}