using System;
using CleanArchitectureTemplate_Application.ServiceContract;
using Hangfire;
using VisionAiChrono.Api.BackgroundJobs;

namespace VisionAiChrono.Api.Services
{
    public class HangfireEmailQueueService : IEmailQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireEmailQueueService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string QueueEmail(string mailTo, string subject, string htmlBody)
        {
            return _backgroundJobClient.Enqueue<EmailJob>(job => job.SendAsync(mailTo, subject, htmlBody));
        }

        public string QueueEmail(string mailTo, string subject, string htmlBody, TimeSpan delay)
        {
            return _backgroundJobClient.Schedule<EmailJob>(job => job.SendAsync(mailTo, subject, htmlBody), delay);
        }
    }
}
