using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VrcUrlPooling.Models;

[Index(nameof(Id), IsUnique = true)]
[Index(nameof(Url), IsUnique = false)]
public abstract class UrlSlotBase
{
    public int Id { get; set; }
    [Column(TypeName = "varchar(5000)")]
    public string? Url { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

[Comment("用于文字和图片")]
public class TextUrlSlot : UrlSlotBase
{
    [Comment("如果有，就当成图片解析并且在返回时保证图片长宽不超过这个大小，max 2048，0 则不作为图片出来")]
    public int MaxSize { get; set; } = 0;
    [Comment("对于图片，允许缓存结果")]
    public bool AllowCache { get; set; } = true;
}

public class VideoUrlSlot : UrlSlotBase {}
