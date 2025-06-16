using System.ComponentModel.DataAnnotations;

namespace MVC.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(20, ErrorMessage = "Username must be between 3 and 20 characters long.", MinimumLength = 3)]
    public string UserName { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = null!;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; } = true;
}
