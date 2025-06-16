using Core.Entities;

namespace MVC.Models;

public class BanViewModel
{
    public string UserName { get; set; } = null!;
    public DateTime? BannedTo { get; set; }
}