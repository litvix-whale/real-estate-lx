using Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role, string? searchTerm = null,
    string? statusFilter = null, string? sortOrder = null, int? skip = null, int? take = null);
    Task<int> GetUsersCountByRoleAsync(string role, string? searchTerm = null, string? statusFilter = null);
    Task<IdentityResult> AddAsync(User user, string password);
    Task<IdentityResult> UpdateAsync(User user, string password);
    Task<string> BanUserAsync(User user, DateTime? bannedTo);
    Task<string> DeleteAsync(User user);
}
