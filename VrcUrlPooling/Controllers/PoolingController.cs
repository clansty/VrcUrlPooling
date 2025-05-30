using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VrcUrlPooling.Services;

namespace VrcUrlPooling.Controllers;

[Route("[action]")]
public class PoolingController(IConfiguration configuration, ILogger<PoolingController> logger, AppDbContext db, UrlRegisterService reg) : Controller
{
    private readonly string secret = configuration.GetValue<string>("Secret")!;
    
    [HttpGet]
    [Route("/{id:int}")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Get(int id)
    {
        var ua = Request.Headers.UserAgent;
        var shouldProxy = false;

        UrlSlotBase? data;
        if (ua.ToString().Contains("NSPlayer"))
        {
            data = await db.VideoUrls.FirstOrDefaultAsync(x => x.Id == id);
        }
        else if (ua.ToString().Contains("UnityWebRequest"))
        {
            data = await db.TextUrls.FirstOrDefaultAsync(x => x.Id == id);
            shouldProxy = true;
        }
        else
        {
            return StatusCode(418);
        }

        if (data is not { Url: not null } || !(data.ExpiresAt > DateTime.UtcNow)) return NotFound();
        if (!shouldProxy) return RedirectPreserveMethod(data.Url);
        // Proxy the request to the URL
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(data.Url);
        var responseStream = await response.Content.ReadAsStreamAsync();
        return new FileStreamResult(responseStream, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
    }

    public record PoolingRequest(string Secret, string Url, string Type);
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] PoolingRequest? request)
    {
        if (request == null || string.IsNullOrEmpty(request.Url))
        {
            return BadRequest();
        }

        // Validate the secret
        if (request.Secret != secret)
        {
            return Unauthorized();
        }

        UrlSlotBase? allocated = null;
        switch (request.Type)
        {
            case "image":
            case "text":
                allocated = await reg.AllocateSlotAsync(db.TextUrls, request.Url);
                break;
            case "video":
                allocated = await reg.AllocateSlotAsync(db.VideoUrls, request.Url);
                break;
            default:
                return BadRequest();
        }

        if (allocated == null)
        {
            return NotFound();
        }
        return Ok(new
        {
            Id = allocated.Id,
            ExpiresAt = allocated.ExpiresAt,
        });
    }
    
    public record BatchPoolingRequest(string Secret, string[] Url, string Type);

    [HttpPost]
    public async Task<IActionResult> RegisterBatch([FromBody] BatchPoolingRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        // Validate the secret
        if (request.Secret != secret)
        {
            return Unauthorized();
        }

        if (request.Url.Length == 0)
        {
            return BadRequest("No URLs provided.");
        }
        
        var allocatedSlots = new List<UrlSlotBase>();
        foreach (var url in request.Url)
        {
            if (string.IsNullOrEmpty(url))
            {
                continue; // Skip empty URLs
            }

            UrlSlotBase? allocated = null;
            switch (request.Type)
            {
                case "image":
                case "text":
                    allocated = await reg.AllocateSlotAsync(db.TextUrls, url);
                    break;
                case "video":
                    allocated = await reg.AllocateSlotAsync(db.VideoUrls, url);
                    break;
                default:
                    return BadRequest($"Invalid type: {request.Type}");
            }

            if (allocated != null)
            {
                allocatedSlots.Add(allocated);
            }
        }

        return Ok(allocatedSlots);
    }
}