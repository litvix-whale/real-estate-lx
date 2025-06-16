namespace Core.Entities;

public class TopicCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TopicId { get; set; }
    public Guid CategoryId { get; set; }
}