using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VrcUrlPooling;

[Index(nameof(Id), IsUnique = true)]
[Index(nameof(Url), IsUnique = false)]
public abstract class UrlSlotBase
{
    public int Id { get; set; }
    [Column(TypeName = "varchar(5000)")]
    public string? Url { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class TextUrlSlot : UrlSlotBase {}
public class VideoUrlSlot : UrlSlotBase {}
