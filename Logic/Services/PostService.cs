using Core.Entities;
using Core.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

namespace Logic.Services
{
    public class PostService(IPostRepository postRepository, IUserPostRepository userPostRepository, IUserRepository userRepository, ICommentRepository commentRepository, ICommentVoteRepository commentVoteRepository, IPostVoteRepository postVoteRepository, IUserTitleRepository userTitleRepository, ITopicRepository topicRepository) : IPostService
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IUserPostRepository _userPostRepository = userPostRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly ICommentRepository _commentRepository = commentRepository;
        private readonly ICommentVoteRepository _commentVoteRepository = commentVoteRepository;
        private readonly IPostVoteRepository _postVoteRepository = postVoteRepository;
        private readonly IUserTitleRepository _userTitleRepository = userTitleRepository;
        private readonly ITopicRepository _topicRepository = topicRepository;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<string> SubscribeUserAsync(string email, string title)
        {
            var post = await _postRepository.GetByTitleAsync(title);
            if (post == null)
            {
                return "Post not found.";
            }

            var user = await _userRepository.GetByUserNameAsync(email);
            if (user == null)
            {
                return "User not found.";
            }

            var userPost = await _userPostRepository.GetByUserIdAndPostIdAsync(user.Id, post.Id);
            if (userPost != null)
            {
                return "User is already subscribed to this post.";
            }

            await _userPostRepository.AddAsync(new UserPost
            {
                UserId = user.Id,
                PostId = post.Id
            });

            return "User subscribed successfully.";
        }

        public async Task<string> UnsubscribeUserAsync(string email, string title)
        {
            var post = await _postRepository.GetByTitleAsync(title);
            if (post == null)
            {
                return "Post not found.";
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return "User not found.";
            }

            var userPost = await _userPostRepository.GetByUserIdAndPostIdAsync(user.Id, post.Id);
            if (userPost == null)
            {
                return "User is not subscribed to this post.";
            }

            await _userPostRepository.DeleteAsync(userPost.Id);

            return "User unsubscribed successfully.";
        }

        public async Task<List<Post>> FindPostsAsync(string title)
        {
            throw new NotImplementedException();
        }

        public async Task<(string, Guid)> AddPostAsync(string title, string text, IFormFile? file, Guid userId, Guid topicId)
        {
            var postId = Guid.NewGuid();
            try
            {
                var (fileContent, fileName, fileType) = await ProcessFileAsync(file);

                await _postRepository.AddAsync(new Post {
                    Id = postId,
                    Title = title,
                    Text = text,
                    FileContent = fileContent,
                    FileName = fileName,
                    FileType = fileType,
                    UserId = userId,
                    TopicId = topicId
                });

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.UserName))
                {
                    throw new InvalidOperationException("User not found or email is null.");
                }
                await SubscribeUserAsync(user.UserName, title);
            }
            catch (Exception ex)
            {
                return ($"Failed to create post. Error: {ex.Message}", Guid.Empty);
            }
            return ("Success", postId);
        }

        public async Task<string> DeletePostAsync(string title)
        {
            var post = await _postRepository.GetByTitleAsync(title);
            if (post == null)
            {
                return "Post not found.";
            }
            try
            {
                await _postRepository.DeleteAsync(post.Id);
            }
            catch (Exception)
            {
                return "Failed to delete post.";
            }
            return "Success";
        }

        public async Task<string> AddCommentAsync(string text, IFormFile? file, Guid userId, Guid postId)
        {
            try
            {
                var (fileContent, fileName, fileType) = await ProcessFileAsync(file);

                await _commentRepository.AddAsync(new Comment
                {
                    Text = text,
                    FileContent = fileContent,
                    FileName = fileName,
                    FileType = fileType,
                    UserId = userId,
                    PostId = postId
                });
            }
            catch (Exception ex)
            {
                return $"Failed to create comment. Error: {ex.Message}";
            }
            return "Success";
        }

        public async Task<string> DeleteCommentAsync(Guid commentId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return "Comment not found.";
            }
            try
            {
                await _commentRepository.DeleteAsync(commentId);
            }
            catch (Exception)
            {
                return "Failed to delete comment.";
            }
            return "Success";
        }

        public async Task<string> DownloadFileAsync(IFormFile file)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Post>> GetAllPosts()
        {
            return await _postRepository.GetAllPosts();
        }

        public async Task<Post> GetPostByIdAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                return null!;
            }

            return post;
        }

        public async Task<List<Comment>> GetAllCommentsAsync(Guid postId)
        {
            return await _commentRepository.GetAllCommentsAsync(postId);
        }

        public async Task<Post> GetPostByTitleAsync(string title)
        {
            return await _postRepository.GetByTitleAsync(title);
        }

        public async Task<List<User>> GetPostSubscribersAsync(Guid postId)
        {
            // Get all user-post relationships for this post
            var userPosts = await _userPostRepository.GetAllAsync();
            userPosts = userPosts.Where(up => up.PostId == postId).ToList();
            
            // Get the actual user objects for each subscription
            var subscribers = new List<User>();
            foreach (var userPost in userPosts)
            {
                var user = await _userRepository.GetByIdAsync(userPost.UserId);
                if (user != null)
                {
                    subscribers.Add(user);
                }
            }

            return subscribers;
        }

        public async Task<bool> IsUserSubscribedAsync(string email, string title)
        {
            try
            {
                var post = await _postRepository.GetByTitleAsync(title);
                if (post == null)
                {
                    return false;
                }

                var user = await _userRepository.GetByUserNameAsync(email) ?? 
                        await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    return false;
                }

                var userPost = await _userPostRepository.GetByUserIdAndPostIdAsync(user.Id, post.Id);
                return userPost != null;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<(string, int)> VoteAsync(Guid commentId, Guid userId, string voteType)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return ("Comment not found.", 0);
            }

            var existingVote = await _commentVoteRepository.GetByCommentIdAndUserIdAsync(commentId, userId);
            if (existingVote != null)
            {
                if (existingVote.VoteType == voteType)
                {
                    if (voteType == "up")
                    {
                        comment.UpVotes--;
                    }
                    else
                    {
                        comment.DownVotes--;
                    }

                    await _commentVoteRepository.DeleteAsync(existingVote.Id);
                }
                else
                {
                    if (voteType == "up")
                    {
                        comment.UpVotes++;
                        comment.DownVotes--;
                    }
                    else
                    {
                        comment.DownVotes++;
                        comment.UpVotes--;
                    }

                    existingVote.VoteType = voteType;
                    await _commentVoteRepository.UpdateAsync(existingVote);
                }
            }
            else
            {
                if (voteType == "up")
                {
                    comment.UpVotes++;
                }
                else
                {
                    comment.DownVotes++;
                }

                await _commentVoteRepository.AddAsync(new CommentVote
                {
                    CommentId = commentId,
                    UserId = userId,
                    VoteType = voteType
                });
            }

            await _commentRepository.UpdateAsync(comment);
            
            // Check if the user of this comment should get a title
            var post = await _postRepository.GetByIdAsync(comment.PostId);
            if (post?.TopicId != null)
            {
                await CheckAndAssignUserTitleAsync(comment.UserId, post.TopicId.Value);
            }
            
            return ("Success", comment.UpVotes - comment.DownVotes);
        }

        // New method to check and assign titles
        private async Task CheckAndAssignUserTitleAsync(Guid userId, Guid topicId)
        {
            // Check if user already has this title
            var existingTitle = await _userTitleRepository.GetByUserIdAndTopicIdAsync(userId, topicId);
            if (existingTitle != null)
            {
                // User already has the title
                return;
            }

            // Get all posts for this topic
            var topicPosts = await _postRepository.GetByTopicIdAsync(topicId);
            
            // Get all comments by this user for posts in this topic
            var userComments = new List<Comment>();
            foreach (var post in topicPosts)
            {
                var postEntity = post as Post;
                if (postEntity == null) continue;
                var comments = await _commentRepository.GetByUserIdAndPostIdAsync(userId, postEntity.Id);
                userComments.AddRange(comments);
            }
            
            // Count comments with rating > 3
            int highRatedComments = userComments.Count(c => (c.UpVotes - c.DownVotes) > 3);
            
            // Check if user qualifies for the title
            if (highRatedComments >= 5)
            {
                var topic = await _topicRepository.GetByIdAsync(topicId);
                if (topic == null)
                {
                    return;
                }
                
                // Create new title
                await _userTitleRepository.AddAsync(new UserTitle
                {
                    UserId = userId,
                    TopicId = topicId,
                    Title = $"{topic.Name} Expert"
                });
            }
        }

        // Additional method for profile page to get user titles
        public async Task<List<UserTitle>> GetUserTitlesAsync(Guid userId)
        {
            return await _userTitleRepository.GetByUserIdAsync(userId);
        }

        public async Task<Dictionary<Guid, string>> GetUserVotesForCommentsAsync(Guid userId, List<Guid> commentIds)
        {
            var votes = await _commentVoteRepository.GetAllByUserIdAndCommentIdsAsync(userId, commentIds);
            
            var userVotes = new Dictionary<Guid, string>();
            
            foreach (var vote in votes)
            {
                userVotes[vote.CommentId] = vote.VoteType;
            }
            
            foreach (var commentId in commentIds)
            {
                if (!userVotes.ContainsKey(commentId))
                {
                    userVotes[commentId] = "none";
                }
            }
            
            return userVotes;
        }

        public async Task<IFormFile?> GetFileAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                return null;
            }

            return ConvertToFormFile(post.FileContent, post.FileName, post.FileType);
        }

        private static IFormFile? ConvertToFormFile(byte[]? fileContent, string? fileName, string? fileType)
        {
            if (fileContent == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileType))
            {
                return null;
            }

            var stream = new MemoryStream(fileContent);
            return new FormFile(stream, 0, fileContent.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = fileType
            };
        }

        private static async Task<(byte[]?, string?, string?)> ProcessFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (null, null, null);
            }

            try 
            {
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    throw new Exception("File size exceeds maximum limit.");
                }

                var allowedFileTypes = new[] { 
                    "image/jpeg", 
                    "image/png", 
                    "image/gif", 
                    "application/pdf", 
                    "text/plain", 
                    "application/msword", 
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                if (!allowedFileTypes.Contains(file.ContentType))
                {
                    throw new Exception("File type not allowed.");
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                
                return (
                    memoryStream.ToArray(), 
                    Path.GetFileName(file.FileName), 
                    file.ContentType
                );
            }
            catch (Exception)
            {
                return (null, null, null);
            }
        }

        public async Task<IFormFile?> GetCommentFileAsync(Guid commentId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return null;
            }

            return ConvertToFormFile(comment.FileContent, comment.FileName, comment.FileType);
        }
        
        public async Task<(string, int)> PostVoteAsync(Guid postId, Guid userId, string userPostVote)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
            return ("Post not found.", 0);
            }

            var existingVote = await _postVoteRepository.GetVoteByUser(postId, userId);
            
            
            if (existingVote != null)
            { 
            if (existingVote.RateType == userPostVote)
            {
                if (userPostVote == "up")
                {
                post.Rating--; 
                }
                else
                {
                post.Rating++; 
                }
                await _postVoteRepository.DeleteAsync(existingVote.Id);
            }
            else
            {
                if (userPostVote == "up")
                {
                post.Rating += 2; 
                }
                else
                {
                post.Rating -= 2; 
                }
                existingVote.RateType = userPostVote; 
                await _postVoteRepository.UpdateAsync(existingVote);
            }
            }
            else
            {
            if (userPostVote == "up")
            {
                post.Rating++; 
            }
            else
            {
                post.Rating--; 
            }
            await _postVoteRepository.AddAsync(new PostVote
            {
                PostId = postId,
                UserId = userId,
                RateType = userPostVote
            });
            }
            await _postRepository.UpdateAsync(post);
            return ("Success", post.Rating);
        }

        public async Task<int> GetRateForPostAsync(Guid postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                return 0;
            }

            return post.Rating;

        }
    }
}
