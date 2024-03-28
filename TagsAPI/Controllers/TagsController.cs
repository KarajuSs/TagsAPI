using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TagsAPI.Data;
using TagsAPI.Model;

[ApiController]
[Route("[controller]")]
public class TagsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TagsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://api.stackexchange.com/2.3/tags?order=desc&min=1000&sort=popular&site=stackoverflow");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            // Odczytaj odpowiedź jako bajty
            var contentBytes = await response.Content.ReadAsByteArrayAsync();

            // Dekompresuj lub zdekoduj odpowiedź, jeśli jest skompresowana lub zakodowana
            var decompressedContent = DecompressIfNeeded(contentBytes);

            // Konwertuj bajty na string
            var content = Encoding.UTF8.GetString(decompressedContent);

            // Sprawdź, czy odpowiedź zawiera poprawne dane JSON
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Empty or invalid response received.");
            }

            var tagsResponse = JsonConvert.DeserializeObject<TagsResponse>(content);

            // Zapisywanie do bazy danych
            foreach (var item in tagsResponse.Items)
            {
                _context.Tags.Add(new Tag { Name = item.Name, Count = item.Count });
            }

            await _context.SaveChangesAsync();

            return Ok(tagsResponse.Items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    private byte[] DecompressIfNeeded(byte[] content)
    {
        // Sprawdź, czy zawartość jest skompresowana lub zakodowana
        // i zdekompresuj/zdekoduj ją, jeśli to konieczne
        // W tym przykładzie zakładamy, że odpowiedź jest skompresowana w formacie GZip
        using (var inputStream = new MemoryStream(content))
        {
            using (var outputStream = new MemoryStream())
            {
                using (var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }
}