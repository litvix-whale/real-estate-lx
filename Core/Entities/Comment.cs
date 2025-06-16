using Microsoft.AspNetCore.Http;

namespace Core.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public byte[]? FileContent { get; set; }
    
    public string? FileName { get; set; }
    
    public string? FileType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; } = Guid.NewGuid();

    public Guid PostId { get; set; } = Guid.NewGuid();

    public int UpVotes { get; set; } = 0;

    public int DownVotes { get; set; } = 0;
}