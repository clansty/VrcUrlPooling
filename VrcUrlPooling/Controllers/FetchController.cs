using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VrcUrlPooling.Models;
using VrcUrlPooling.Services;

namespace VrcUrlPooling.Controllers;

[Route("[action]")]
public class FetchController(ILogger<FetchController> logger, AppDbContext db, ImageScaleService iss) : Controller
{
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

        if (data is TextUrlSlot { MaxSize: > 0 } ts)
        {
            try
            {
                var image = await iss.GetOrDownloadImageAsync(data.Url, ts.MaxSize, ts.AllowCache);
                return File(image, "image/png");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "下载和压缩图片失败");
            }
        }

        // Proxy the request to the URL
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(data.Url);
        var responseStream = await response.Content.ReadAsStreamAsync();
        return new FileStreamResult(responseStream, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
    }
}