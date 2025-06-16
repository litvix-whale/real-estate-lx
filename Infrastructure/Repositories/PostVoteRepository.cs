using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories
{
    public class PostVoteRepository(AppDbContext context) : RepositoryBase<PostVote>(context), IPostVoteRepository
    {
        public async Task<PostVote> GetVoteByUser(Guid postId, Guid userId)
        {
            var result = await _context.PostVotes.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);
            return result!;
        }
    }
}