using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using VisionAiChrono.Application.Dtos.Pipeline;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Enumration;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class PipelineService : IPipelineService
    {
        private const string HistoryIncludes =
            "Pipeline,PipelineRunModels,PipelineRunVideos,PipelineRunVideos.PipelineResults,Exports";

        private readonly IUnitOfWork _unitOfWork;

        public PipelineService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PipelineResponseDTO> CreateAsync(CreatePipelineDTO dto)
        {
            var pipeline = new Pipeline
            {
                Name = dto.Name,
                Description = dto.Description
            };

            await _unitOfWork.Repository<Pipeline>().AddAsync(pipeline);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(pipeline);
        }

        public async Task<PipelineResponseDTO> GetByIdAsync(Guid id)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>()
                .GetByIdAsync(id, "PipelineModels,PipelineModels.AiModel");

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {id} not found.");

            return MapToResponse(pipeline);
        }

        public async Task<IEnumerable<PipelineResponseDTO>> GetAllAsync()
        {
            var pipelines = await _unitOfWork.Repository<Pipeline>()
                .GetAllAsync("PipelineModels,PipelineModels.AiModel");

            return pipelines.Select(MapToResponse).ToList();
        }

        public async Task<PagedResultDTO<PipelineRunHistoryResponseDTO>> GetRunHistoryAsync(
            Guid pipelineId,
            int pageNumber = 1,
            int pageSize = 20,
            ExecutionStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(pipelineId);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {pipelineId} not found.");

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            var (data, totalCount) = await _unitOfWork.Repository<PipelineRun>().GetPagedWithCountAsync(
                pageNumber,
                pageSize,
                r => r.PipelineId == pipelineId && (status == null || r.Status == status),
                q => q.OrderByDescending(r => r.StartedAt),
                HistoryIncludes,
                cancellationToken);

            return new PagedResultDTO<PipelineRunHistoryResponseDTO>
            {
                Data = data.Select(MapRunToHistory).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        private static PipelineRunHistoryResponseDTO MapRunToHistory(PipelineRun run)
        {
            var results = run.PipelineRunVideos?
                .SelectMany(v => v.PipelineResults ?? new List<PipelineResult>())
                .ToList() ?? new List<PipelineResult>();

            var confidences = results.Where(r => r.Confidence.HasValue).Select(r => r.Confidence!.Value).ToList();

            return new PipelineRunHistoryResponseDTO
            {
                Id = run.Id,
                PipelineId = run.PipelineId,
                PipelineName = run.Pipeline?.Name ?? string.Empty,
                StartedByName = run.StartedBy?.FullName,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                Status = run.Status.ToString(),
                TotalMedia = run.PipelineRunVideos?.Count ?? 0,
                TotalModels = run.PipelineRunModels?.Count ?? 0,
                TotalResults = results.Count,
                AverageConfidence = confidences.Count == 0 ? null : confidences.Average(),
                HasExport = run.Exports is { Count: > 0 },
                DurationSeconds = run.CompletedAt.HasValue
                    ? (int)Math.Round((run.CompletedAt.Value - run.StartedAt).TotalSeconds)
                    : 0
            };
        }

        private static string ResolveTaskName(int taskIndex)
        {
            var task = VisionAiChrono.Application.VisionDetection.Models.DetectionTaskExtensions
                .FromIndex(taskIndex);

            return task?.ToString() ?? string.Empty;
        }

        private static PipelineResponseDTO MapToResponse(Pipeline pipeline)
        {
            return new PipelineResponseDTO
            {
                Id = pipeline.Id,
                Name = pipeline.Name,
                Description = pipeline.Description,
                CreatedAt = pipeline.CreatedAt,
                UpdatedAt = pipeline.UpdatedAt,
                PipelineModels = pipeline.PipelineModels?.Select(pm => new PipelineModelResponseDTO
                {
                    Id = pm.Id,
                    PipelineId = pm.PipelineId,
                    AiModelId = pm.AiModelId,
                    AiModelName = pm.AiModel?.Name ?? string.Empty,
                    TaskIndex = pm.TaskIndex,
                    TaskName = pm.AiModel?.Name ?? ResolveTaskName(pm.TaskIndex),
                    Order = pm.Order,
                    ConfigurationJson = pm.ConfigurationJson
                }).ToList() ?? new List<PipelineModelResponseDTO>()
            };
        }
    }
}
