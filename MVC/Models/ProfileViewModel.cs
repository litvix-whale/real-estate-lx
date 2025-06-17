using System.ComponentModel.DataAnnotations;
using Core.Entities;

namespace MVC.Models;

public class ProfileViewModel
{
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? ProfilePicture { get; set; }
    
    // For profile picture selection
    public List<string> AvailableProfilePictures { get; set; } = new List<string>
    {
        "pfp_1.png", "pfp_2.png", "pfp_3.png", "pfp_4.png", "pfp_5.png", 
        "pfp_6.png", "pfp_7.png", "pfp_8.png", "pfp_9.png", "pfp_10.png"
    };
    
    // For updating profile
    [Required]
    [Display(Name = "Username")]
    public string? NewUserName { get; set; }
    
    [Required]
    [Display(Name = "Profile Picture")]
    public string? NewProfilePicture { get; set; }
}