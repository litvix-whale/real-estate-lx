using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? BannedTo { get; set; }
    public string ProfilePicture { get; set; } = "pfp_1.png";
    public virtual ICollection<UserTitle> Titles { get; set; } = new List<UserTitle>();
    public virtual ICollection<RealEstate> RealEstates { get; set; } = new List<RealEstate>();
}