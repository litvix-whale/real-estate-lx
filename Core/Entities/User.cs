using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ProfilePicture { get; set; } = "pfp_1.png";
    public virtual ICollection<RealEstate> RealEstates { get; set; } = new List<RealEstate>();
}