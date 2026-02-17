using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Interfaces.Services;

namespace UrlShrt.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        private async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml) bodyBuilder.HtmlBody = body;
            else bodyBuilder.TextBody = body;
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(emailSettings["Host"], int.Parse(emailSettings["Port"]!), bool.Parse(emailSettings["UseSsl"]!), cancellationToken);
            await client.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"], cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        public async Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
            => await SendEmailAsync(to, subject, body, isHtml, cancellationToken);

        public async Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
        {
            var html = $"""
            <h2>Password Reset Request</h2>
            <p>Click the link below to reset your password. This link expires in 1 hour.</p>
            <a href="{resetLink}" style="padding:10px 20px;background:#4F46E5;color:white;text-decoration:none;border-radius:5px;">Reset Password</a>
            <p>If you didn't request this, please ignore this email.</p>
            """;
            await SendAsync(to, "Reset Your Password", html, cancellationToken: cancellationToken);
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmLink, CancellationToken cancellationToken = default)
        {
            var html = $"""
            <h2>Confirm Your Email</h2>
            <p>Thank you for registering! Please confirm your email address.</p>
            <a href="{confirmLink}" style="padding:10px 20px;background:#4F46E5;color:white;text-decoration:none;border-radius:5px;">Confirm Email</a>
            """;
            await SendAsync(to, "Confirm Your Email Address", html, cancellationToken: cancellationToken);
        }

        public async Task SendWelcomeEmailAsync(string to, string fullName, CancellationToken cancellationToken = default)
        {
            var html = $"""
            <h2>Welcome, {fullName}!</h2>
            <p>Your account has been created successfully. Start shortening URLs today!</p>
            """;
            await SendAsync(to, "Welcome to URL Shortener!", html, cancellationToken: cancellationToken);
        }
    }
}
