using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisionAiChrono.Application.Dtos.Pipeline;
using VisionAiChrono.Application.Dtos.PipelineRun;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IPipelineService
    {
        Task<PipelineResponseDTO> CreateAsync(CreatePipelineDTO dto);

        Task<PipelineResponseDTO> GetByIdAsync(Guid id);

        Task<IEnumerable<PipelineResponseDTO>> GetAllAsync();

        Task<PagedResultDTO<PipelineRunHistoryResponseDTO>> GetRunHistoryAsync(
            Guid pipelineId,
            int pageNumber = 1,
            int pageSize = 20,
            Domain.Enumration.ExecutionStatus? status = null,
            CancellationToken cancellationToken = default);
    }
}
