namespace Core.Entities;

public class UserTopic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
}