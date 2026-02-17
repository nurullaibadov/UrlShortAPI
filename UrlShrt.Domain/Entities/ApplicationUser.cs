using Microsoft.AspNetCore.Identity;  // ← Bunu dəyiş

namespace UrlShrt.Domain.Entities
{
    public class ApplicationUser : IdentityUser  // IdentityUser indi Microsoft.AspNetCore.Identity-dən gəlir
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? TimeZone { get; set; } = "UTC";
        public bool IsActive { get; set; } = true;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public int UrlLimit { get; set; } = 100; // Free plan default
        public string Plan { get; set; } = "Free"; // Free, Pro, Enterprise

        // Navigation
        public ICollection<ShortenedUrl> ShortenedUrls { get; set; } = new List<ShortenedUrl>();
    }
}
