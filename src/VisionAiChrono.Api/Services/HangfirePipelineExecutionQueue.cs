using System;
using Hangfire;
using VisionAiChrono.Api.BackgroundJobs;
using VisionAiChrono.Application.ServiceContract;

namespace VisionAiChrono.Api.Services
{
    public class HangfirePipelineExecutionQueue : IPipelineExecutionQueue
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfirePipelineExecutionQueue(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string QueueExecution(Guid pipelineRunId)
        {
            return _backgroundJobClient
                .Enqueue<PipelineExecutionJob>(job => job.ExecuteAsync(pipelineRunId, CancellationToken.None));
        }
    }
}
