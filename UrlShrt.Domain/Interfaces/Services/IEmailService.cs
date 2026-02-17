using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlShrt.Domain.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
        Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
        Task SendConfirmationEmailAsync(string to, string confirmLink, CancellationToken cancellationToken = default);
        Task SendWelcomeEmailAsync(string to, string fullName, CancellationToken cancellationToken = default);
    }
}
