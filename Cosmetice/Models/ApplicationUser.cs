using Microsoft.AspNetCore.Identity;

namespace Cosmetice.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }

        public string? SkinType { get; set; }

        public string? Country { get; set; }

        public int? Age { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
}