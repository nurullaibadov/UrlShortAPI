using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Domain.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        string GenerateShortCode(int length = 6);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
