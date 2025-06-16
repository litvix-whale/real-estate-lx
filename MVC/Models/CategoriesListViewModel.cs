using Core.Entities;

namespace MVC.Models;

public class CategoriesListViewModel
{
    public List<Category> Categories { get; set; } = new();
}