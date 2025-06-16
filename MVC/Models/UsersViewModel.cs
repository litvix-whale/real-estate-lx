using Core.Entities;

namespace MVC.Models;

public class UsersViewModel
{
    public IEnumerable<User> Users { get; set; } = null!;

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 10;
    public int TotalUsers { get; set; }

    public string CurrentFilter { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";

    public string SortOrder { get; set; } = string.Empty;

    public string UsernameSortParam => string.IsNullOrEmpty(SortOrder) ? "username_desc" : SortOrder == "username_desc" ? "username_asc" : "username_desc";
    public string DateSortParam => SortOrder == "date_asc" ? "date_desc" : "date_asc";
}
