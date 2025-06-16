using Core.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;

namespace Infrastructure.Seed
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDatabase(AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            string adminPassword)
        {
            // Seed Roles
            await SeedRoles(roleManager);

            // Seed Users
            await SeedUsers(userManager, adminPassword);

            // Seed Categories
            await SeedCategories(context);

            // Seed Topics
            await SeedTopics(context);

            // Connect Topics to Categories
            await SeedTopicCategories(context);

            // Seed User Topic Subscriptions
            await SeedUserTopics(context, userManager);

            // Seed Posts
            await SeedPosts(context, userManager);

            // Seed Comments
            await SeedComments(context, userManager);

            // Seed Comment Votes
            await SeedCommentVotes(context, userManager);

            // Seed User Posts (bookmarks)
            await SeedUserPosts(context, userManager);

            // Seed Notifications
            await SeedNotifications(context, userManager);

            await SeedRealEstates(context, userManager);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
        {
            string[] roleNames = { "Admin", "Default" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

        private static async Task SeedUsers(UserManager<User> userManager, string adminPassword)
        {
            // Seed Admin
            await SeedAdminUser(userManager, adminPassword);

            // Seed Regular Users
            var userSeedData = new List<(string username, string email)>
            {
                ("johndoe", "john@example.com"),
                ("janedoe", "jane@example.com"),
                ("bobsmith", "bob@example.com"),
                ("alicesmith", "alice@example.com")
            };

            foreach (var userData in userSeedData)
            {
                await SeedRegularUser(userManager, userData.username, userData.email);
            }
        }

        private static async Task SeedAdminUser(UserManager<User> userManager, string adminPassword)
        {
            var existingAdmin = await userManager.FindByEmailAsync("admin@example.com");

            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    UserName = "Admin",
                    Email = "admin@admin.com",
                    EmailConfirmed = true,
                    ProfilePicture = "pfp_6.png",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task SeedRegularUser(UserManager<User> userManager, string username, string email)
        {
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser == null)
            {
                var user = new User
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true,
                    ProfilePicture = "pfp_5.png",
                    CreatedAt = DateTime.UtcNow,
                };

                var result = await userManager.CreateAsync(user, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Default");
                }
            }
        }

        private static async Task SeedCategories(AppDbContext context)
        {
            var categoriesToAdd = new[]
            {
                "Technology", "Science", "Arts", "Sports", "Health",
                "Gaming", "Food", "Travel", "Entertainment", "Education"
            };

            foreach (var categoryName in categoriesToAdd)
            {
                var existingCategory = await context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName);

                if (existingCategory == null)
                {
                    var newCategory = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = categoryName
                    };
                    context.Categories.Add(newCategory);
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedTopics(AppDbContext context)
        {
            var topicsToAdd = new[]
            {
                "Programming", "Artificial Intelligence", "Astronomy",
                "Football", "Basketball", "Fitness", "Cooking", "Painting"
            };

            foreach (var topicName in topicsToAdd)
            {
                var existingTopic = await context.Topics
                    .FirstOrDefaultAsync(t => t.Name == topicName);

                if (existingTopic == null)
                {
                    var newTopic = new Topic
                    {
                        Id = Guid.NewGuid(),
                        Name = topicName,
                        CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 30))
                    };
                    context.Topics.Add(newTopic);
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedTopicCategories(AppDbContext context)
        {
            var topicCategoryMappings = new[]
            {
                ("Programming", "Technology"),
                ("Artificial Intelligence", "Technology"),
                ("Artificial Intelligence", "Science"),
                ("Astronomy", "Science"),
                ("Football", "Sports"),
                ("Basketball", "Sports"),
                ("Fitness", "Health"),
                ("Cooking", "Food"),
                ("Painting", "Arts")
            };

            foreach (var (topicName, categoryName) in topicCategoryMappings)
            {
                var topic = await context.Topics
                    .FirstOrDefaultAsync(t => t.Name == topicName);

                var category = await context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName);

                if (topic != null && category != null)
                {
                    var existingMapping = await context.TopicCategories
                        .FirstOrDefaultAsync(tc =>
                            tc.TopicId == topic.Id &&
                            tc.CategoryId == category.Id);

                    if (existingMapping == null)
                    {
                        context.TopicCategories.Add(new TopicCategory
                        {
                            Id = Guid.NewGuid(),
                            TopicId = topic.Id,
                            CategoryId = category.Id
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        // Remaining seed methods would follow similar patterns
        // (UserTopics, Posts, Comments, etc.)
        // I've omitted them for brevity, but they would use the same approach
        // of checking for existence before adding

        private static async Task SeedUserTopics(AppDbContext context, UserManager<User> userManager)
        {
            var users = await userManager.Users.ToListAsync();
            var topics = await context.Topics.ToListAsync();
            var random = new Random();

            foreach (var user in users)
            {
                var existingUserTopics = await context.UserTopics
                    .Where(ut => ut.UserId == user.Id)
                    .ToListAsync();

                if (existingUserTopics.Count == 0)
                {
                    var topicsToSubscribe = topics
                        .OrderBy(x => random.Next())
                        .Take(random.Next(2, 5))
                        .ToList();

                    foreach (var topic in topicsToSubscribe)
                    {
                        context.UserTopics.Add(new UserTopic
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            TopicId = topic.Id
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedPosts(AppDbContext context, UserManager<User> userManager)
        {
            var users = await userManager.Users.ToListAsync();
            var topics = await context.Topics.ToListAsync();
            var random = new Random();

            foreach (var topic in topics)
            {
                var existingPosts = await context.Posts
                    .Where(p => p.TopicId == topic.Id)
                    .ToListAsync();

                if (existingPosts.Count == 0)
                {
                    // Create 3-5 posts per topic
                    int postCount = random.Next(3, 6);
                    for (int i = 0; i < postCount; i++)
                    {
                        var user = users[random.Next(users.Count)];

                        var newPost = new Post
                        {
                            Id = Guid.NewGuid(),
                            Title = $"{topic.Name} Post {i + 1}",
                            Text = $"This is a sample post about {topic.Name}. Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                            UserId = user.Id,
                            TopicId = topic.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                        };

                        context.Posts.Add(newPost);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedComments(AppDbContext context, UserManager<User> userManager)
        {
            var users = await userManager.Users.ToListAsync();
            var posts = await context.Posts.ToListAsync();
            var random = new Random();

            foreach (var post in posts)
            {
                var existingComments = await context.Comments
                    .Where(c => c.PostId == post.Id)
                    .ToListAsync();

                if (existingComments.Count == 0)
                {
                    // Create 2-4 comments per post
                    int commentCount = random.Next(2, 5);
                    for (int i = 0; i < commentCount; i++)
                    {
                        var user = users[random.Next(users.Count)];

                        var newComment = new Comment
                        {
                            Id = Guid.NewGuid(),
                            Text = $"This is a sample comment on the post. Random commentary {i + 1}.",
                            PostId = post.Id,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                            UpVotes = random.Next(0, 10),
                            DownVotes = random.Next(0, 5)
                        };

                        context.Comments.Add(newComment);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedCommentVotes(AppDbContext context, UserManager<User> userManager)
        {
            var users = await userManager.Users.ToListAsync();
            var comments = await context.Comments.ToListAsync();
            var random = new Random();

            foreach (var comment in comments)
            {
                var existingVotes = await context.CommentVotes
                    .Where(cv => cv.CommentId == comment.Id)
                    .ToListAsync();

                if (existingVotes.Count == 0)
                {
                    // Create 1-3 votes per comment
                    int voteCount = random.Next(1, 4);
                    var usedUsers = new HashSet<Guid>();

                    for (int i = 0; i < voteCount; i++)
                    {
                        User user;
                        do
                        {
                            user = users[random.Next(users.Count)];
                        } while (usedUsers.Contains(user.Id));

                        usedUsers.Add(user.Id);

                        var newCommentVote = new CommentVote
                        {
                            Id = Guid.NewGuid(),
                            CommentId = comment.Id,
                            UserId = user.Id,
                            VoteType = random.Next(2) == 0 ? "up" : "down",
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                        };

                        context.CommentVotes.Add(newCommentVote);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedUserPosts(AppDbContext context, UserManager<User> userManager)
        {
            var users = await userManager.Users.ToListAsync();
            var posts = await context.Posts.ToListAsync();
            var random = new Random();

            foreach (var user in users)
            {
                var existingUserPosts = await context.UserPosts
                    .Where(up => up.UserId == user.Id)
                    .ToListAsync();

                if (existingUserPosts.Count == 0)
                {
                    // Create 1-3 user post bookmarks per user
                    int bookmarkCount = random.Next(1, 4);
                    var usedPosts = new HashSet<Guid>();

                    for (int i = 0; i < bookmarkCount; i++)
                    {
                        Post post;
                        do
                        {
                            post = posts[random.Next(posts.Count)];
                        } while (usedPosts.Contains(post.Id));

                        usedPosts.Add(post.Id);

                        var newUserPost = new UserPost
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            PostId = post.Id
                        };

                        context.UserPosts.Add(newUserPost);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedNotifications(AppDbContext context, UserManager<User> userManager)
        {
            var users = await userManager.Users.ToListAsync();
            var posts = await context.Posts.ToListAsync();
            var topics = await context.Topics.ToListAsync();
            var random = new Random();

            foreach (var user in users)
            {
                var existingNotifications = await context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .ToListAsync();

                if (existingNotifications.Count == 0)
                {
                    // Create 2-4 notifications per user
                    int notificationCount = random.Next(2, 5);

                    for (int i = 0; i < notificationCount; i++)
                    {
                        var notificationType = random.Next(2);
                        var newNotification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            IsRead = random.Next(2) == 0,
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                        };

                        if (notificationType == 0 && posts.Any())
                        {
                            // Post-related notification
                            var post = posts[random.Next(posts.Count)];
                            newNotification.PostId = post.Id;
                            newNotification.Message = $"New comment on your post: {post.Title}";
                        }
                        else if (topics.Any())
                        {
                            // Topic-related notification
                            var topic = topics[random.Next(topics.Count)];
                            newNotification.TopicId = topic.Id;
                            newNotification.Message = $"New post in topic: {topic.Name}";
                        }

                        context.Notifications.Add(newNotification);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedRealEstates(AppDbContext context, UserManager<User> userManager)
        {
            if (context.RealEstates.Any()) return;

            var user = await userManager.FindByEmailAsync("john@example.com");
            if (user == null) return;

            var estates = new List<RealEstate>
    {
        new RealEstate
        {
            Title = "Оренда 1 кімнатної квартири на Степанівни",
            Description = "Квартира з ремонтом, зручна локація, інтернет, меблі.",
            IsNewBuilding = true,
            Category = RealEstateCategoryEnum.Residential,
            RealtyType = RealEstateTypeEnum.Apartment,
            Deal = DealTypeEnum.Rent,
            Country = "Україна",
            Region = "Львівська область",
            Locality = "Львів",
            Borough = "Залізничний",
            Street = "Степанівни",
            StreetType = "вулиця",
            Latitude = 49.8428706,
            Longitude = 23.9994757,
            Floor = 3,
            TotalFloors = 7,
            AreaTotal = 68.7f,
            AreaLiving = 20f,
            AreaKitchen = 38.02f,
            RoomCount = 1,
            NewBuildingName = "ЖК Manhattan",
            Price = 600,
            Currency = CurrencyEnum.USD,
            ImageUrls = new List<string>
            {
                "https://crm-08498194.s3.eu-west-1.amazonaws.com/zahid-rent/estate-images/eef2b874cce8e541ba533a107b08affb.jpg",
                "https://crm-08498194.s3.eu-west-1.amazonaws.com/zahid-rent/estate-images/5de324f815eb0fb522d8a0d645a52cf0.jpg"
            },
            UserId = user.Id
        },
        new RealEstate
        {
            Title = "Оренда 3-кім. квартири. вул. Личаківська-Тракт Глинянський",
            Description = "Оренда 3-кім. квартири. вул. Личаківська-Тракт Глинянський. Ремонт косметичний. Кімнати ізольовані. Меблі та побутова техніка. Два бойлера. Два засклених балкона. Ванна. Інтернет. Відеоогляд.",
            IsNewBuilding = false,
            Category = RealEstateCategoryEnum.Residential,
            RealtyType = RealEstateTypeEnum.Apartment,
            Deal = DealTypeEnum.Rent,
            Country = "Україна",
            Region = "Львівська область",
            Locality = "Львів",
            Borough = "Личаківський",
            Street = "Глинянський Тракт",
            StreetType = "вулиця",
            Latitude = 49.8375986,
            Longitude = 24.0764789,
            Floor = 4,
            TotalFloors = 9,
            AreaTotal = 70,
            AreaKitchen = 11,
            RoomCount = 3,
            Price = 13500,
            Currency = CurrencyEnum.UAH,
            ImageUrls = new List<string>
            {
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/563a5ac32b5ad721e29a54202fe1dae6.jpg",
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/24fa51495afa45ef10d793382f915478.jpg",
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/8c1c78da7e399ef9c8563ca48df67e46.jpg",
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/6657f4d5a23acb9b78e9ea28410db85b.jpg",
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/278ec410d9c77addf6d94d803d706b65.jpg",
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/24b6ed9b6c89eecdf32121b1cb241be5.jpg",
                "https://recrm21.s3.eu-west-1.amazonaws.com/2024/07/845d47b8ecf6961bf943152abb04b4f1.jpg"
            },
            UserId = user.Id
        }
    };

            await context.RealEstates.AddRangeAsync(estates);
            await context.SaveChangesAsync();
        }

    }
}