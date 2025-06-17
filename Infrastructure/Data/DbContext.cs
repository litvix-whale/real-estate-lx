using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = null;
            }

            if (entry.State == EntityState.Modified)
            {
                // Забороняємо змінювати CreatedAt вручну
                entry.Property(e => e.CreatedAt).IsModified = false;

                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

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
