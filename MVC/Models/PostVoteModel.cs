namespace MVC.Models;

public class PostVoteModel
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string VoteType { get; set; } = string.Empty;
}