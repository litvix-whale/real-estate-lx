using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices;

namespace Core.Interfaces;


public interface IPostService
{
    Task<string> SubscribeUserAsync(string name, string title);
    Task<string> UnsubscribeUserAsync(string name, string title);

    Task<List<Post>> FindPostsAsync(string title);

    Task<(string, Guid)> AddPostAsync(string title, string text, IFormFile? file, Guid userId, Guid topicId);

    Task<string> DeletePostAsync(string title);

    Task<string> AddCommentAsync(string text, IFormFile? file, Guid userId, Guid postId);

    Task<string> DeleteCommentAsync(Guid commentId);

    Task<string> DownloadFileAsync(IFormFile file);

    Task<List<Post>> GetAllPosts();

    Task<Post> GetPostByIdAsync(Guid postId);

    Task<List<Comment>> GetAllCommentsAsync(Guid postId);

    Task<Post> GetPostByTitleAsync(string title);

    Task<List<User>> GetPostSubscribersAsync(Guid postId);

    Task<bool> IsUserSubscribedAsync(string email, string title);

    Task<(string, int)> VoteAsync(Guid commentId, Guid userId, string voteType);

    Task<Dictionary<Guid, string>> GetUserVotesForCommentsAsync(Guid userId, List<Guid> commentIds);

    Task<IFormFile?> GetFileAsync(Guid postId);
    Task<IFormFile?> GetCommentFileAsync(Guid commentId);
    Task<(string, int)> PostVoteAsync(Guid postId, Guid userId, string UserPostVote);
    Task<int> GetRateForPostAsync(Guid postId);
    Task<List<UserTitle>> GetUserTitlesAsync(Guid userId);
}