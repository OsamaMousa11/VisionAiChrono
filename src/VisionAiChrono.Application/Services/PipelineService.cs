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

            return pipelines.Select(MapToResponse);
        }

        public async Task<PipelineResponseDTO> UpdateAsync(Guid id, UpdatePipelineDTO dto)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(id);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {id} not found.");

            if (dto.Name is not null)
                pipeline.Name = dto.Name;

            if (dto.Description is not null)
                pipeline.Description = dto.Description;

            _unitOfWork.Repository<Pipeline>().Update(pipeline);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(pipeline);
        }

        public async Task DeleteAsync(Guid id)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(id);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {id} not found.");

            _unitOfWork.Repository<Pipeline>().Delete(pipeline);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<PipelineModelResponseDTO> AddModelAsync(Guid pipelineId, AddPipelineModelDTO dto)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(pipelineId);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {pipelineId} not found.");

            var aiModel = await _unitOfWork.Repository<AiModel>().GetByIdAsync(dto.AiModelId);

            if (aiModel is null)
                throw new NotFoundException($"AI Model with ID {dto.AiModelId} not found.");

            var pipelineModel = new PipelineModel
            {
                PipelineId = pipelineId,
                AiModelId = dto.AiModelId,
                Order = dto.Order,
                ConfigurationJson = dto.ConfigurationJson
            };

            await _unitOfWork.Repository<PipelineModel>().AddAsync(pipelineModel);
            await _unitOfWork.CompleteAsync();

            return new PipelineModelResponseDTO
            {
                Id = pipelineModel.Id,
                PipelineId = pipelineModel.PipelineId,
                AiModelId = pipelineModel.AiModelId,
                AiModelName = aiModel.Name,
                Order = pipelineModel.Order,
                ConfigurationJson = pipelineModel.ConfigurationJson
            };
        }

        public async Task RemoveModelAsync(Guid pipelineId, Guid modelId)
        {
            var pipelineModel = await _unitOfWork.Repository<PipelineModel>()
                .FindAsync(pm => pm.PipelineId == pipelineId && pm.AiModelId == modelId);

            if (pipelineModel is null)
                throw new NotFoundException($"Pipeline Model not found for Pipeline {pipelineId} and AI Model {modelId}.");

            _unitOfWork.Repository<PipelineModel>().Delete(pipelineModel);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<PipelineRunDetailResponseDTO> CloneAsDraftAsync(Guid pipelineId, string userId, string? newName = null)
        {
            var source = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(pipelineId, "PipelineModels");

            if (source is null)
                throw new NotFoundException($"Pipeline with ID {pipelineId} not found.");

            var draft = new Pipeline
            {
                Name = string.IsNullOrWhiteSpace(newName) ? $"{source.Name} (copy)" : newName,
                Description = source.Description
            };

            foreach (var model in (source.PipelineModels ?? new List<PipelineModel>()).OrderBy(m => m.Order))
            {
                draft.PipelineModels.Add(new PipelineModel
                {
                    PipelineId = draft.Id,
                    AiModelId = model.AiModelId,
                    Order = model.Order,
                    ConfigurationJson = model.ConfigurationJson
                });
            }

            await _unitOfWork.Repository<Pipeline>().AddAsync(draft);
            await _unitOfWork.CompleteAsync();

            return new PipelineRunDetailResponseDTO
            {
                Id = draft.Id,
                PipelineId = draft.Id,
                PipelineName = draft.Name,
                Status = "Draft",
                CreatedAt = draft.CreatedAt
            };
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

            const string includes =
                "Pipeline,PipelineRunModels,PipelineRunVideos,PipelineRunVideos.PipelineResults,Exports";

            var (data, totalCount) = await _unitOfWork.Repository<PipelineRun>().GetPagedWithCountAsync(
                pageNumber,
                pageSize,
                r => r.PipelineId == pipelineId && (status == null || r.Status == status),
                q => q.OrderByDescending(r => r.StartedAt),
                includes,
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
                    Order = pm.Order,
                    ConfigurationJson = pm.ConfigurationJson
                }).ToList() ?? new List<PipelineModelResponseDTO>()
            };
        }
    }
}
