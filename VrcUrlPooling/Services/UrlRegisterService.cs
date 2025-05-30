using Microsoft.EntityFrameworkCore;
using VrcUrlPooling.Models;

namespace VrcUrlPooling.Services;

public class UrlRegisterService(ILogger<UrlRegisterService> logger, AppDbContext db)
{
    public async Task<UrlSlotBase?> AllocateSlotAsync<T>(DbSet<T> dbSet, string url, int imageSize = 0, bool allowCache = true)
        where T : UrlSlotBase
    {
        using var tx = await db.Database.BeginTransactionAsync();
        var entityType = db.Model.FindEntityType(typeof(T));

        // Step 1: 查找是否已有绑定
        var existingSlot = await dbSet
            .FromSqlRaw($"SELECT * FROM `{entityType!.GetTableName()!}` WHERE Url = {{0}} AND ExpiresAt > NOW() FOR UPDATE", url)
            .FirstOrDefaultAsync();

        if (existingSlot is not null)
        {
            existingSlot.ExpiresAt = DateTime.UtcNow.AddDays(1);

            if (existingSlot is TextUrlSlot ts)
            {
                ts.AllowCache &= allowCache;
                ts.MaxSize = Math.Max(imageSize, ts.MaxSize);
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return existingSlot;
        }

        // 找一个空闲槽位（过期或者未分配）
        var availableSlot = await dbSet
            .FromSqlRaw($"SELECT * FROM `{entityType!.GetTableName()!}` WHERE (Url IS NULL OR ExpiresAt < NOW()) LIMIT 1 FOR UPDATE")
            .FirstOrDefaultAsync();

        if (availableSlot is not null)
        {
            availableSlot.Url = url;
            availableSlot.ExpiresAt = DateTime.UtcNow.AddDays(1);

            if (availableSlot is TextUrlSlot ts)
            {
                ts.AllowCache = allowCache;
                ts.MaxSize = imageSize;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return availableSlot;
        }

        return null;
    }

    public async Task<List<UrlSlotBase>> BatchAllocateSlotsAsync<T>(DbSet<T> dbSet, IEnumerable<string> urls, int imageSize = 0, bool allowCache = false)
        where T : UrlSlotBase, new()
    {
        var urlList = urls.Distinct().ToList();
        if (urlList.Count == 0)
        {
            return [];
        }

        using var tx = await db.Database.BeginTransactionAsync();
        var entityType = db.Model.FindEntityType(typeof(T));
        var tableName = entityType!.GetTableName()!;
        var result = new List<UrlSlotBase>();

        try
        {
            // 1. 查找所有已存在的绑定
            var existingSlots = await dbSet
                .FromSqlRaw($"SELECT * FROM `{tableName}` WHERE Url IN ({string.Join(",", urlList.Select((_, i) => $"{{{i}}}"))}) AND ExpiresAt > NOW() FOR UPDATE",
                    urlList.ToArray())
                .ToListAsync();

            var existingUrls = existingSlots.Select(s => s.Url).ToHashSet();
            var remainingUrls = urlList.Where(url => !existingUrls.Contains(url)).ToList();

            // 更新已存在绑定的过期时间
            foreach (var slot in existingSlots)
            {
                slot.ExpiresAt = DateTime.UtcNow.AddDays(1);

                if (slot is TextUrlSlot ts)
                {
                    ts.AllowCache &= allowCache;
                    ts.MaxSize = Math.Max(imageSize, ts.MaxSize);
                }

                result.Add(slot);
            }

            if (remainingUrls.Count != 0)
            {
                // 2. 查找空闲槽位
                var availableSlots = await dbSet
                    .FromSqlRaw($"SELECT * FROM `{tableName}` WHERE (Url IS NULL OR ExpiresAt < NOW()) LIMIT {remainingUrls.Count} FOR UPDATE")
                    .ToListAsync();

                // 3. 分配空闲槽位
                for (int i = 0; i < Math.Min(availableSlots.Count, remainingUrls.Count); i++)
                {
                    var slot = availableSlots[i];
                    var url = remainingUrls[i];
                    slot.Url = url;
                    slot.ExpiresAt = DateTime.UtcNow.AddDays(1);

                    if (slot is TextUrlSlot ts)
                    {
                        ts.AllowCache = allowCache;
                        ts.MaxSize = imageSize;
                    }

                    result.Add(slot);
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}