using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Core.Entities;

namespace MVC.Controllers;

[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;
    

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int count = 7, string searchQuery = "")
    {
        var categories = await _categoryService.GetCategories(page, count, searchQuery);
        var model = new CategoriesListViewModel
        {
            Categories = new List<Category>()
        };

        foreach (var category in categories)
        {
            model.Categories.Add(category);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(string name)
    {
        if (ModelState.IsValid)
        {
            var result = await _categoryService.CreateCategoryAsync(name);

            if (result == "Success")
            {
                return RedirectToAction("Index", "Category");
            }

            ModelState.AddModelError(string.Empty, result);
        }

        return View(name);
    }   

    [HttpGet]
    public IActionResult Delete()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string name)
    {
        if (ModelState.IsValid)
        {
            var result = await _categoryService.DeleteCategoryAsync(name);

            if (result == "Success")
            {
                return RedirectToAction("Index", "Category");
            }

            ModelState.AddModelError(string.Empty, result);
        }

        return View(name);
    }
}