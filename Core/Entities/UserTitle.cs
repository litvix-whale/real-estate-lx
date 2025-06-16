namespace Core.Entities;

public class UserTitle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Topic Topic { get; set; } = null!;
}