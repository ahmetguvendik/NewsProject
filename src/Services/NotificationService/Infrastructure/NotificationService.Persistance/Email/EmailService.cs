using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;

namespace NotificationService.Persistance.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue<int>("Smtp:Port", 587);
        var username = _configuration["Smtp:Username"] ?? string.Empty;
        var password = _configuration["Smtp:Password"] ?? string.Empty;
        var fromName = _configuration["Smtp:FromName"] ?? "News Portal";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(username, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        await client.SendMailAsync(message, cancellationToken);

        _logger.LogInformation("Email sent to {Email} with subject '{Subject}'.", toEmail, subject);
    }
}
