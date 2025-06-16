using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Topic> Topics { get; set; }
    public DbSet<UserTopic> UserTopics { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostVote> PostVotes { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TopicCategory> TopicCategories { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserPost> UserPosts { get; set; }
    public DbSet<CommentVote> CommentVotes { get; set; }
    public DbSet<UserTitle> UserTitles { get; set; }
    public DbSet<RealEstate> RealEstates { get; set; }
    public DbSet<RealEstateImage> RealEstateImages { get; set; }
}
