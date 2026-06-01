using System.Net;
using System.Net.Mail;
using BiliTool.Vn.Application.Services;
using Microsoft.Extensions.Configuration;

namespace BiliTool.Vn.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        // For security in a real system we should use Options pattern and appsettings, 
        // but here we use the exact credentials provided in the user request.
        var fromEmail = "drninhmai@gmail.com";
        var appPassword = "gfwy fnfo xnea ounz";

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(fromEmail, appPassword),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, "BiliTool.Vn"),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }
}
