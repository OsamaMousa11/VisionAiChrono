using System;

namespace CleanArchitectureTemplate_Application.ServiceContract
{
    public interface IEmailQueueService
    {
        string QueueEmail(string mailTo, string subject, string htmlBody);

        string QueueEmail(string mailTo, string subject, string htmlBody, TimeSpan delay);
    }
}
