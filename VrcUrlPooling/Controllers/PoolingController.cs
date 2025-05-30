using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VrcUrlPooling.Models;
using VrcUrlPooling.Services;

namespace VrcUrlPooling.Controllers;

[Route("[action]")]
public class PoolingController(IConfiguration configuration, ILogger<PoolingController> logger, AppDbContext db, UrlRegisterService reg) : Controller
{
    private readonly string secret = configuration.GetValue<string>("Secret")!;

    public record PoolingRequest(string Secret, string Url, string Type, int MaxSize = 0, bool AllowCache = true);
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
                allocated = await reg.AllocateSlotAsync(db.TextUrls, request.Url, request.MaxSize, request.AllowCache);
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
    
    public record BatchPoolingRequest(string Secret, string[] Url, string Type, int MaxSize = 0, bool AllowCache = true);

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

        var allocatedSlots = request.Type switch
        {
            "image" or "text" => await reg.BatchAllocateSlotsAsync(db.TextUrls, request.Url, request.MaxSize, request.AllowCache),
            "video" => await reg.BatchAllocateSlotsAsync(db.VideoUrls, request.Url),
            _ => throw new InvalidOperationException($"Invalid type: {request.Type}"),
        };

        return Ok(allocatedSlots);
    }
}