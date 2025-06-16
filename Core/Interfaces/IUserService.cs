using System.Security.Claims;
using Core.Entities;

namespace Core.Interfaces;

public interface IUserService
{
    Task<string> RegisterUserAsync(string email, string userName, string password, bool rememberMe);
    Task<string> LoginUserAsync(string email, string password, bool rememberMe);
    Task<string> DeleteUserAsync(string email);
    Task<User> GetUserByEmailAsync(string email);
    Task<User> GetUserByUserNameAsync(string userName);
    Task<string> BanUserAsync(string email, DateTime? bannedTo);
    Task<string> UnbanUserAsync(string email);
    string? GetBannedTo(Guid userId);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role, string? searchTerm = null,
    string? statusFilter = null, string? sortOrder = null, int? skip = null, int? take = null);
    Task<int> GetUsersCountByRoleAsync(string role, string? searchTerm = null, string? statusFilter = null);
    Task AddClaimsAsync(User user, IEnumerable<Claim> claims);
    Task RemoveClaimAsync(User user, string claimType);
    Task LogoutUserAsync();
    Task<User> GetUserByIdAsync(Guid userId);
    Task<string> GetUserName(User user);
    string GetUserPfp(User user);
    Task<string> UpdateUserProfileAsync(User user, string userName, string profilePicture);
    Task<string> GetUserIdByName(string userName);
    Task<bool> IsUserAdmin(string userName);
}