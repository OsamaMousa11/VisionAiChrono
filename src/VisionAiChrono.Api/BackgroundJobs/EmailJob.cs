using System;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Application.ServiceContract;
using Hangfire;

namespace VisionAiChrono.Api.BackgroundJobs
{
    public class EmailJob
    {
        private readonly IMailingService _mailService;
        private readonly ILogger<EmailJob> _logger;

        public EmailJob(IMailingService mailService, ILogger<EmailJob> logger)
        {
            _mailService = mailService;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 3)]
        public async Task SendAsync(string mailTo, string subject, string htmlBody)
        {
            try
            {
                _logger.LogInformation("Hangfire email job started for {MailTo} with subject {Subject}", mailTo, subject);

                await _mailService.SendMessageAsync(mailTo, subject, htmlBody, null);

                _logger.LogInformation("Hangfire email job completed for {MailTo}", mailTo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hangfire email job failed for {MailTo}", mailTo);
                throw;
            }
        }
    }
}
