using Microsoft.EntityFrameworkCore;

namespace VrcUrlPooling;

public class AppDbContext : DbContext
{
    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public virtual DbSet<TextUrlSlot> TextUrls { get; set; }
    public virtual DbSet<VideoUrlSlot> VideoUrls { get; set; }
}