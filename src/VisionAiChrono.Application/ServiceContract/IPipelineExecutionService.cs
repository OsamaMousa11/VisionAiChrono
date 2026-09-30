using System;
using System.Threading;
using System.Threading.Tasks;
using VisionAiChrono.Application.Dtos.PipelineRun;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IPipelineExecutionService
    {
        /// <summary>
        /// Runs every selected task against every uploaded media file, persists the
        /// results, builds the Excel export and returns what was detected.
        /// </summary>
        Task<PipelineExecutionSummaryDTO> ExecuteRunAsync(
            Guid pipelineRunId, CancellationToken cancellationToken = default);
    }
}
