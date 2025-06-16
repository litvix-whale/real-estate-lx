using Core.Entities;

namespace MVC.Models;

public class PostDetailsViewModel
{
    public Post Post { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public Dictionary<Guid, string> UserNames { get; set; } = new();
    public Dictionary<Guid, string> UserVotes { get; set; } = new();
    public Dictionary<Guid, string> UserProfilePictures { get; set; } = new();
    public string UserPostVote { get; set; } = string.Empty;
    public Dictionary<string,string> UserPost { get; set; } = new();
    public Dictionary<Guid, List<string>> UserTitles { get; set; } = new();
}