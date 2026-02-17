using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Entities
{
    public class CustomDomain : BaseEntity
    {
        public string DomainName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        // Navigation
        public ApplicationUser? User { get; set; }
    }
}
