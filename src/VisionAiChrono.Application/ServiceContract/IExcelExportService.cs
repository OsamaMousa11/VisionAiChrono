using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IExcelExportService
    {
        Task<(byte[] Content, string FileName)> BuildRunResultsAsync(
            Guid pipelineRunId,
            CancellationToken cancellationToken = default);
    }
}
