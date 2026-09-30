using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VisionAiChrono.Application.ServiceContract;

namespace VisionAiChrono.Api.BackgroundJobs
{
    /// <summary>
    /// Thin Hangfire wrapper around <see cref="IPipelineExecutionService"/> so the
    /// same execution logic can run in the background or inline from the API.
    /// </summary>
    public class PipelineExecutionJob
    {
        private readonly IPipelineExecutionService _executionService;
        private readonly ILogger<PipelineExecutionJob> _logger;

        public PipelineExecutionJob(
            IPipelineExecutionService executionService,
            ILogger<PipelineExecutionJob> logger)
        {
            _executionService = executionService;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAsync(Guid pipelineRunId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pipeline execution job started for run {RunId}", pipelineRunId);

            var summary = await _executionService.ExecuteRunAsync(pipelineRunId, cancellationToken);

            _logger.LogInformation(
                "Pipeline run {RunId} finished. {Succeeded}/{Total} succeeded, {Detections} detection(s).",
                pipelineRunId, summary.Succeeded, summary.TotalExecutions, summary.TotalDetections);
        }
    }
}
