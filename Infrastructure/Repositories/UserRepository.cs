using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class UserRepository(AppDbContext context, UserManager<User> userManager) : RepositoryBase<User>(context), IUserRepository
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<IdentityResult> AddAsync(User user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, "Default");
        return result;
    }

    public async override Task AddAsync(User user)
    {
        await _userManager.CreateAsync(user);
    }

    public async Task<IdentityResult> UpdateAsync(User user, string password)
    {
        await _userManager.UpdateAsync(user);
        await _userManager.RemovePasswordAsync(user);
        await _userManager.AddPasswordAsync(user, password);
        return IdentityResult.Success;
    }

    public async Task<string> DeleteAsync(User user)
    {
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? "Success" : string.Join(", ", result.Errors.Select(e => e.Description));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role, string? searchTerm = null,
                string? statusFilter = null, string? sortOrder = null, int? skip = null, int? take = null)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
        var query = usersInRole.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u =>
                u.UserName != null && u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Email != null && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        query = sortOrder switch
        {
            "username_asc" => query.OrderBy(u => u.UserName),
            "username_desc" => query.OrderByDescending(u => u.UserName),
            "date_asc" => query.OrderBy(u => u.CreatedAt),
            "date_desc" => query.OrderByDescending(u => u.CreatedAt),
            _ => query.OrderBy(u => u.UserName) // Default sort
        };

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return query.ToList();
    }

    public async Task<int> GetUsersCountByRoleAsync(string role, string? searchTerm = null, string? statusFilter = null)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
        var query = usersInRole.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u =>
                u.UserName != null && u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Email != null && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return query.Count();
    }
}
