using Microsoft.AspNetCore.Http;

namespace Core.Entities;

public class PostVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string RateType { get; set; } = string.Empty;
}