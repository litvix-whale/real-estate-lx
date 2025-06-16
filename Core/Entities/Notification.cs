using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class Notification
{
    [Key]
    public Guid Id { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public Guid UserId { get; set; }
    public Guid? PostId { get; set; }
    public Guid? TopicId { get; set; }
}