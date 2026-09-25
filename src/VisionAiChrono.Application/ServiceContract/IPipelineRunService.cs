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
        Task<PipelineRunResponseDTO> CreateAsync(CreatePipelineRunDTO dto, string userId);

        Task<RunExecutionResponseDTO> ExecuteAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default);

        Task<PipelineRunResponseDTO> GetByIdAsync(Guid id);

        Task<PipelineRunDetailResponseDTO> GetDetailAsync(Guid id);

        Task<IEnumerable<PipelineResultResponseDTO>> GetResultsAsync(Guid id);

        Task<PagedResultDTO<PipelineRunHistoryResponseDTO>> GetHistoryAsync(
            string userId,
            int pageNumber = 1,
            int pageSize = 20,
            Guid? pipelineId = null,
            Domain.Enumration.ExecutionStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<PipelineRunDetailResponseDTO> UpdateAsync(
            Guid id,
            UpdatePipelineRunDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default);

        Task<RunExecutionResponseDTO> ReRunAsync(Guid id, string userId, CancellationToken cancellationToken = default);

        Task<(byte[] Content, string FileName, string ContentType)> GetLatestExportAsync(Guid id);

        Task<PipelineRunResponseDTO> UpdateStatusAsync(Guid id, UpdatePipelineRunStatusDTO dto);

        Task<PipelineRunVideoResponseDTO> AddVideoAsync(Guid runId, AddPipelineRunVideoDTO dto);

        Task<PipelineRunModelResponseDTO> AddModelAsync(Guid runId, AddPipelineRunModelDTO dto);

        Task RemoveVideoAsync(Guid runId, Guid videoId);

        Task RemoveModelAsync(Guid runId, Guid modelId);

        Task<IEnumerable<PipelineRunResponseDTO>> GetAllByUserAsync(string userId);
    }
}
