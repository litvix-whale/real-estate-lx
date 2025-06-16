using Microsoft.AspNetCore.Http;

namespace Core.Entities;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public byte[]? FileContent { get; set; }
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CommentId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TopicId { get; set; }
    public int Rating {get; set;} = 0;
}