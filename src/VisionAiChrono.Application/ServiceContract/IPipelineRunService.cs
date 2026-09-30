using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisionAiChrono.Application.Dtos.PipelineResult;
using VisionAiChrono.Application.Dtos.PipelineRun;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IPipelineRunService
    {
        Task<RunExecutionResponseDTO> ExecuteAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default);

        Task<RunExecutionResponseDTO> QueueExecutionAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default);

        Task<PipelineRunDetailResponseDTO> GetDetailAsync(Guid id);

        Task<IEnumerable<PipelineResultResponseDTO>> GetResultsAsync(Guid id);

        Task<(byte[] Content, string FileName, string ContentType)> GetLatestExportAsync(Guid id);
    }
}
