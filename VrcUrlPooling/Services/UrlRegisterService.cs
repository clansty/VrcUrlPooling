using Microsoft.EntityFrameworkCore;

namespace VrcUrlPooling.Services;

public class UrlRegisterService(ILogger<UrlRegisterService> logger, AppDbContext db)
{
    public async Task<UrlSlotBase?> AllocateSlotAsync<T>(DbSet<T> dbSet, string url)
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
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return availableSlot;
        }

        return null;
    }
}