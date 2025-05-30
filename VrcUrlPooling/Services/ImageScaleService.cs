using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace VrcUrlPooling.Services;

public class ImageScaleService(ILogger<ImageScaleService> logger)
{
    private readonly ConcurrentDictionary<(string, int), Lazy<Task<byte[]>>> _inProgress = new();

    public Task<byte[]> GetOrDownloadImageAsync(string url, int size, bool allowCache)
    {
        var lazyTask = _inProgress.GetOrAdd((url, size), _ => new Lazy<Task<byte[]>>(async () =>
        {
            try
            {
                if (allowCache && TryGetFromCache(url, size, out var cachedImage))
                    return cachedImage;

                var image = await DownloadAndResizeAsync(url, size);
                if (allowCache)
                    await CacheImage(url, size, image);
                return image;
            }
            finally
            {
                _inProgress.TryRemove((url, size), out var _);
            }
        }));

        return lazyTask.Value;
    }

    private async Task<byte[]> DownloadAndResizeAsync(string url, int size)
    {
        // 从 url 下载图片
        using var httpClient = new HttpClient();
        var imageBytes = await httpClient.GetByteArrayAsync(url);

        // 使用SixLabors.ImageSharp处理图片
        using var image = Image.Load(imageBytes);

        // 验证最长遍是否超过 size
        int maxDimension = Math.Max(image.Width, image.Height);

        // 如果超过，缩放图片最长边到 size
        if (maxDimension <= size)
        {
            return imageBytes;
        }

        logger.LogInformation("压缩图片 {origin} -> {size}", maxDimension, size);
        float ratio = (float)size / maxDimension;
        int newWidth = (int)(image.Width * ratio);
        int newHeight = (int)(image.Height * ratio);

        image.Mutate(x => x.Resize(newWidth, newHeight));

        // 将处理后的图片转换为字节数组
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.Level7,
        });
        return ms.ToArray();
    }

    private async Task CacheImage(string url, int size, byte[] image)
    {
        // Cache/sha256(url)_size.png
        string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
        Directory.CreateDirectory(cacheDir);

        string urlHash = ComputeSha256Hash(url);
        string cacheFilePath = Path.Combine(cacheDir, $"{urlHash}_{size}.png");

        await File.WriteAllBytesAsync(cacheFilePath, image);
    }

    private bool TryGetFromCache(string url, int size, out byte[] image)
    {
        string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
        string urlHash = ComputeSha256Hash(url);

        // 查找特定尺寸的缓存文件
        if (!Directory.Exists(cacheDir))
        {
            image = Array.Empty<byte>();
            return false;
        }

        string cacheFilePath = Path.Combine(cacheDir, $"{urlHash}_{size}.png");
        if (File.Exists(cacheFilePath))
        {
            image = File.ReadAllBytes(cacheFilePath);
            return true;
        }

        image = Array.Empty<byte>();
        return false;
    }

    private string ComputeSha256Hash(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        byte[] hashBytes = sha256.ComputeHash(bytes);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}