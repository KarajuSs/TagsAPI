using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TagsAPI.Data;
using TagsAPI.Model;
using System.IO.Compression;
using System.Linq.Expressions;

[ApiController]
[Route("[controller]")]
public class TagsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TagsController(AppDbContext context)
    {
        _context = context;
    }

    // Metoda do pobierania tagów z API StackOverflow
    [HttpGet("fetch")]
    public async Task<IActionResult> FetchTags()
    {
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://api.stackexchange.com/2.3/tags?order=desc&min=1000&sort=popular&site=stackoverflow");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            // Sprawdź, czy odpowiedź jest skompresowana
            if (response.Content.Headers.ContentEncoding.Any(x => x.ToLower() == "gzip"))
            {
                // Jeśli odpowiedź jest skompresowana w formacie GZip, dekompresuj ją
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream))
                {
                    var content = await reader.ReadToEndAsync();
                    var tagsResponse = JsonConvert.DeserializeObject<TagsResponse>(content);
                    await _context.Tags.AddRangeAsync(tagsResponse.Items);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Jeśli odpowiedź nie jest skompresowana, odczytaj ją normalnie
                var content = await response.Content.ReadAsStringAsync();
                var tagsResponse = JsonConvert.DeserializeObject<TagsResponse>(content);
                await _context.Tags.AddRangeAsync(tagsResponse.Items);
                await _context.SaveChangesAsync();
            }

            return Ok("Tags fetched successfully.");
        }
        catch (Exception ex)
        {
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