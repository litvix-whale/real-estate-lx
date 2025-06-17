using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Core.Entities;
using Core.Interfaces;
using Logic.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Infrastructure.Repositories;
using Logic.Hubs;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Infrastructure.Seed;
using Infrastructure.Xml.Interfaces;
using Infrastructure.Xml.Services;
using Infrastructure.Xml.Configs;
using Microsoft.Extensions.Options;


Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["GoogleMaps:ApiKey"] = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Environment.IsDevelopment()
        ? Env.GetString("LOCAL_CONNECTION_STRING")
        : Env.GetString("CONNECTION_STRING");
    options.UseNpgsql(connectionString);
});

// Конфігурувати XmlDataSettings з ENV
builder.Services.Configure<XmlDataSettings>(options =>
{
    // Завантажити з appsettings
    builder.Configuration.GetSection(XmlDataSettings.SectionName).Bind(options);

    // Перезаписати FeedUrl з ENV
    options.FeedUrl = Env.GetString("XML_FEED_URL");
});

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddSignalR();


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRealEstateRepository, RealEstateRepository>();
builder.Services.AddScoped<IRealEstateService, RealEstateService>();

builder.Services.AddHttpClient<IXmlDataService, XmlDataService>();
builder.Services.AddScoped<IXmlDataService, XmlDataService>();

builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
    });

builder.Services.AddAuthorization();


var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Apply migrations synchronously first
        dbContext.Database.Migrate();

        await using var asyncScope = services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var asyncServices = asyncScope.ServiceProvider;
        var xmlSettings = asyncServices.GetRequiredService<IOptions<XmlDataSettings>>().Value;

        await DatabaseSeeder.SeedDatabase(
            asyncServices.GetRequiredService<AppDbContext>(),
            asyncServices.GetRequiredService<UserManager<User>>(),
            asyncServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            asyncServices.GetRequiredService<IXmlDataService>(),
            xmlSettings,
            Env.GetString("ADMIN_PASSWORD")
        );
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seeding failed");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=RealEstate}/{action=Index}");

app.Run();
