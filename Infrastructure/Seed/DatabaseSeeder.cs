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
            await SeedRoles(roleManager);

            await SeedUsers(userManager, adminPassword);

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
            Images = new List<RealEstateImage>
            {
                new() { Url = "https://.../image1.jpg", UiPriority = 0 },
                new() { Url = "https://.../image2.jpg", UiPriority = 1 }
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
            Images = new List<RealEstateImage>
            {
                new() { Url = "https://.../image3.jpg", UiPriority = 0 },
                new() { Url = "https://.../image4.jpg", UiPriority = 1 },
                new() { Url = "https://.../image5.jpg", UiPriority = 2 }
            },
            UserId = user.Id
        }
    };

            await context.RealEstates.AddRangeAsync(estates);
            await context.SaveChangesAsync();
        }

    }
}