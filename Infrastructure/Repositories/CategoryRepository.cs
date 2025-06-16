using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : RepositoryBase<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories.ToListAsync();
    }

    public async Task<Category> GetByNameAsync(string name)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with name '{name}' not found.");
        }
        return category;
    }

    public async Task<IEnumerable<Category>> GetCategoiresByNameAsync(string name)
    {
        return await _context.Categories.Where(c => c.Name.Contains(name)).ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetCategories(int start, int count, string searchQuery)
    {
        return await _context.Categories
            .Where(c => c.Name.Contains(searchQuery))
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetCategoriesCountAsync(string searchQuery)
    {
        return await _context.Categories
            .Where(c => c.Name.Contains(searchQuery))
            .CountAsync();
    }
}