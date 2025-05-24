using Microsoft.EntityFrameworkCore;

namespace VrcUrlPooling;

[Index(nameof(Id), IsUnique = true)]
[Index(nameof(Url))]
public abstract class UrlSlotBase
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class TextUrlSlot : UrlSlotBase {}
public class VideoUrlSlot : UrlSlotBase {}
