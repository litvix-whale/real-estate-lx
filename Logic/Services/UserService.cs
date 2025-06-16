using System.Security.Claims;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Logic.Services
{
    public class UserService(IUserRepository userRepository, SignInManager<User> signInManager, UserManager<User> userManager) : IUserService
    {
        private readonly IUserRepository userRepository = userRepository;
        private readonly SignInManager<User> _signInManager = signInManager;
        private readonly UserManager<User> _userManager = userManager;

        public async Task<string> RegisterUserAsync(string email, string userName, string password, bool rememberMe)
        {
            var user = new User { UserName = userName, Email = email };
            var result = await userRepository.AddAsync(user, password);
            if (result.Succeeded)
            {
                await LoginUserAsync(user, password, rememberMe);
                return "Success";
            }
            return string.Join(", ", result.Errors.Select(e => e.Description));
        }

        public async Task<string> LoginUserAsync(string email, string password, bool rememberMe)
        {
            var user = await userRepository.GetByEmailAsync(email) ?? await userRepository.GetByUserNameAsync(email);
            if (user == null)
            {
                return "User not found.";
            }
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, user.UserName!));
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            var result = await LoginUserAsync(user, password, rememberMe);
            if (result.Succeeded)
            {
                return "Success";
            }
            if (result.IsLockedOut)
            {
                return "User is locked out.";
            }
            if (result.IsNotAllowed)
            {
                return "User is not allowed to sign in.";
            }
            if (result.RequiresTwoFactor)
            {
                return "Two-factor authentication is required.";
            }
            return "Invalid email or password.";
        }

        public async Task<string> DeleteUserAsync(string email)
        {
            var user = await userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return "User not found.";
            }
            var result = await userRepository.DeleteAsync(user);
            return result;
        }

        public async Task LogoutUserAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role, string? searchTerm = null,
            string? statusFilter = null, string? sortOrder = null, int? skip = null, int? take = null)
        {
            return await userRepository.GetUsersByRoleAsync(role, searchTerm, statusFilter, sortOrder, skip, take);
        }

        public async Task<int> GetUsersCountByRoleAsync(string role, string? searchTerm = null, string? statusFilter = null)
        {
            return await userRepository.GetUsersCountByRoleAsync(role, searchTerm, statusFilter);
        }

        private async Task<SignInResult> LoginUserAsync(User user, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(user.UserName))
            {
                throw new InvalidOperationException("UserName cannot be null or empty.");
            }
            return await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: false);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await userRepository.GetByEmailAsync(email) ?? throw new InvalidOperationException("User not found.");
        }

        public async Task<User> GetUserByUserNameAsync(string userName)
        {
            return await userRepository.GetByUserNameAsync(userName) ?? throw new InvalidOperationException("User not found.");
        }

        public async Task<string> BanUserAsync(string email, DateTime? bannedTo)
        {
            var user = await userRepository.GetByUserNameAsync(email);
            if (user == null)
            {
                return "User not found.";
            }
            bannedTo = bannedTo?.ToUniversalTime();
            return await userRepository.BanUserAsync(user, bannedTo);
        }

        public async Task<string> UnbanUserAsync(string email)
        {
            var user = await userRepository.GetByUserNameAsync(email);
            if (user == null)
            {
                return "User not found.";
            }
            user.BannedTo = null;
            await userRepository.UpdateAsync(user);
            return "Success";
        }
        public async Task AddClaimsAsync(User user, IEnumerable<Claim> claims)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            foreach (var claim in claims)
            {
                var result = await _userManager.AddClaimAsync(user, claim);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to add claim {claim.Type} to user {user.Email}");
                }
            }
        }
        public async Task RemoveClaimAsync(User user, string claimType)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(claimType))
            {
                throw new ArgumentNullException(nameof(claimType));
            }

            var existingClaims = await _userManager.GetClaimsAsync(user);

            var claimToRemove = existingClaims.FirstOrDefault(c => c.Type == claimType);
            if (claimToRemove != null)
            {
                var result = await _userManager.RemoveClaimAsync(user, claimToRemove);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to remove claim {claimType} from user {user.Email}");
                }
            }
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            var user =  await userRepository.GetByIdAsync(userId);

            return user!;
        }

        public async Task<string> GetUserName(User user)
        {
            return await _userManager.GetUserNameAsync(user) ?? throw new InvalidOperationException("User not found.");
        }

        public string GetUserPfp(User user)
        {
            return user.ProfilePicture;
        }

        public async Task<string> UpdateUserProfileAsync(User user, string userName, string profilePicture)
        {
            // Validate the profile picture
            var validProfilePictures = new[]
            {
                "pfp_1.png", "pfp_2.png", "pfp_3.png", "pfp_4.png", "pfp_5.png",
                "pfp_6.png", "pfp_7.png", "pfp_8.png", "pfp_9.png", "pfp_10.png"
            };
            
            if (!validProfilePictures.Contains(profilePicture))
            {
                return "Invalid profile picture selected.";
            }
            
            // Store the old username to check if it changed
            string oldUserName = user.UserName ?? string.Empty;
            bool usernameChanged = !string.Equals(oldUserName, userName);
            
            // Check if username is already taken by another user
            if (usernameChanged)
            {
                var existingUser = await _userManager.FindByNameAsync(userName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return "Username is already taken.";
                }
                
                // Update username
                user.UserName = userName;
            }
            
            // Update profile picture
            user.ProfilePicture = profilePicture;
            
            // Save changes
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                // Only update claims if username changed
                if (usernameChanged)
                {
                    // Update claims to reflect the new username
                    var existingClaims = await _userManager.GetClaimsAsync(user);
                    var nameClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                    var nameIdentifierClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (nameClaim != null)
                    {
                        await _userManager.RemoveClaimAsync(user, nameClaim);
                        await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, userName));
                    }
                    else if (nameIdentifierClaim != null)
                    {
                        await _userManager.RemoveClaimAsync(user, nameIdentifierClaim);
                        await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    }
                    else
                    {
                        await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, userName));
                        await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    }
                    
                    // Important: Sign in the user again with the new username to refresh the principal
                    await _signInManager.RefreshSignInAsync(user);
                }
                
                return "Success";
            }
            
            return string.Join(", ", result.Errors.Select(e => e.Description));
        }

        public async Task<string> GetUserIdByName(string userName)
        {
            var user = await userRepository.GetByUserNameAsync(userName);
            if (user == null)
            {
                return "User not found.";
            }
            return user.Id.ToString();
        }

        public string? GetBannedTo(Guid userId)
        {
            var user = userRepository.GetByIdAsync(userId).Result;
            if (user == null)
            {
                return null;
            }
            if (user.BannedTo == null)
            {
                return null;
            }
            return user.BannedTo.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public async Task<bool> IsUserAdmin(string userName)
        {
            var user = await userRepository.GetByUserNameAsync(userName);
            if (user == null)
            {
                return false;
            }
            var roles = await _userManager.GetRolesAsync(user);
            return roles.Contains("Admin");
        }
    }
}