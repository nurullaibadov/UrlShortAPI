using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Application.DTOs.Admin
{
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public string Plan { get; set; } = "Free";
        public int UrlLimit { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public long TotalUrls { get; set; }
        public long TotalClicks { get; set; }
    }

    public record UpdateUserRoleDto(string UserId, string Role);
    public record UpdateUserStatusDto(string UserId, bool IsActive);
    public record UpdateUserPlanDto(string UserId, string Plan, int UrlLimit);

    public record CreateUserByAdminDto(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role,
        string Plan = "Free",
        int UrlLimit = 100
    );
}
