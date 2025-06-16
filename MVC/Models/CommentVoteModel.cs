using Core.Entities;

namespace MVC.Models;

public class CommentVoteModel
{
    public Guid CommentId { get; set; }

    public Guid UserId { get; set; }

    public string VoteType { get; set; } = string.Empty;
}