using Core.Entities;

namespace MVC.Models;

public class AddPostViewModel
{
    public string Title { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string? File { get; set; }
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public List<Topic> AvailableTopics { get; set; } = [];
}