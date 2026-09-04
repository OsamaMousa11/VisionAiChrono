using Karaakeb.Core.DTO.AuthenticationDTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using CleanArchitectureTemplate_Application.ServiceContract;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace CleanArchitectureTemplate_Application.Services
{
    public class MailingService : IMailingService
    {
        private readonly MailSettings _mailSettings;
        public MailingService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }
        public async Task SendMessageAsync(string mailTo, string subject, string body, IList<IFormFile>? attach)
        {
            var email = new MimeMessage();
            // From
            email.From.Add(new MailboxAddress(
        _mailSettings.DisplayName,
        _mailSettings.SenderEmail));
            // To
            email.To.Add(MailboxAddress.Parse(mailTo));
            email.Subject = subject;
            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            // Attachments
            if (attach is not null && attach.Any())
            {
                foreach (var file in attach)
                {
                    if (file.Length > 0)
                    {
                        using var stream = new MemoryStream();
                        await file.CopyToAsync(stream);
                        builder.Attachments.Add(
                            file.FileName,
                            stream.ToArray(),
                            ContentType.Parse(file.ContentType));
                    }
                }
            }
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(
                    _mailSettings.Host,
                    _mailSettings.Port,
                    SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(
                    _mailSettings.Email,
                    _mailSettings.Password);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}

