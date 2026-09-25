using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using VisionAiChrono.Application.Dtos.PipelineResult;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Enumration;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class PipelineRunService : IPipelineRunService
    {
        private const string FullIncludes =
            "Pipeline,StartedBy,PipelineRunModels,PipelineRunModels.AiModel," +
            "PipelineRunVideos,PipelineRunVideos.Video," +
            "PipelineRunVideos.PipelineResults,PipelineRunVideos.PipelineResults.AiModel,Exports";

        private const string HistoryIncludes =
            "Pipeline,PipelineRunModels,PipelineRunVideos," +
            "PipelineRunVideos.PipelineResults,Exports";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPipelineExecutionQueue _executionQueue;
        private readonly IMediaStorageService _mediaStorage;

        public PipelineRunService(
            IUnitOfWork unitOfWork,
            IPipelineExecutionQueue executionQueue,
            IMediaStorageService mediaStorage)
        {
            _unitOfWork = unitOfWork;
            _executionQueue = executionQueue;
            _mediaStorage = mediaStorage;
        }

        public async Task<PipelineRunResponseDTO> CreateAsync(CreatePipelineRunDTO dto, string userId)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(dto.PipelineId);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {dto.PipelineId} not found.");

            var run = new PipelineRun
            {
                PipelineId = dto.PipelineId,
                StartedById = ParseUserId(userId),
                Status = ExecutionStatus.Pending
            };

            await _unitOfWork.Repository<PipelineRun>().AddAsync(run);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(run);
        }

        public async Task<RunExecutionResponseDTO> ExecuteAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (files is null || files.Count == 0)
                throw new BadRequestException("At least one video or image file is required.");

            if (dto.Models is null || dto.Models.Count == 0)
                throw new BadRequestException("At least one model is required.");

            var modelSelections = dto.Models
                .Select((m, index) => new { Model = m, Index = index })
                .OrderBy(x => x.Model.Order)
                .ThenBy(x => x.Index)
                .Select(x => x.Model)
                .ToList();

            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(dto.PipelineId, cancellationToken: cancellationToken);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {dto.PipelineId} not found.");

            ValidateFiles(files);

            var modelIds = modelSelections.Select(s => s.AiModelId).ToList();
            var models = await _unitOfWork.Repository<AiModel>()
                .FindAllAsync(m => modelIds.Contains(m.Id), cancellationToken: cancellationToken);

            var foundModelIds = models.Select(m => m.Id).ToHashSet();
            var missingModels = modelIds.Where(id => !foundModelIds.Contains(id)).ToList();

            if (missingModels.Count > 0)
                throw new NotFoundException($"AI Model not found: {string.Join(", ", missingModels)}");

            var savedMedia = await SaveMediaAsync(files, cancellationToken);

            var run = new PipelineRun
            {
                PipelineId = dto.PipelineId,
                StartedById = ParseUserId(userId),
                Status = ExecutionStatus.Pending
            };

            await _unitOfWork.Repository<PipelineRun>().AddAsync(run, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await AttachRunContentAsync(run, savedMedia.Select(m => m.MediaId).ToList(), modelSelections, cancellationToken);

            var jobId = _executionQueue.QueueExecution(run.Id);

            return new RunExecutionResponseDTO
            {
                RunId = run.Id,
                JobId = jobId,
                Status = run.Status.ToString(),
                UploadedCount = savedMedia.Count,
                ModelCount = modelSelections.Count,
                Message = $"Queued {savedMedia.Count} media item(s) across {modelSelections.Count} model(s).",
                UploadedMedia = savedMedia
            };
        }

        public async Task<PipelineRunResponseDTO> GetByIdAsync(Guid id)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(id, FullIncludes);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            return MapToResponse(run);
        }

        public async Task<PipelineRunDetailResponseDTO> GetDetailAsync(Guid id)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(id, FullIncludes);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            var results = FlattenResults(run);

            return new PipelineRunDetailResponseDTO
            {
                Id = run.Id,
                PipelineId = run.PipelineId,
                PipelineName = run.Pipeline?.Name ?? string.Empty,
                StartedById = run.StartedById,
                StartedByName = run.StartedBy?.FullName,
                StartedByEmail = run.StartedBy?.Email,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                Status = run.Status.ToString(),
                ErrorMessage = null,
                CreatedAt = run.CreatedAt,
                TotalMedia = run.PipelineRunVideos?.Count ?? 0,
                TotalModels = run.PipelineRunModels?.Count ?? 0,
                TotalResults = results.Count,
                AverageConfidence = AverageConfidence(results),
                PipelineRunVideos = run.PipelineRunVideos?.Select(MapVideoToResponse).ToList()
                    ?? new List<PipelineRunVideoResponseDTO>(),
                PipelineRunModels = run.PipelineRunModels?.Select(MapModelToResponse).ToList()
                    ?? new List<PipelineRunModelResponseDTO>(),
                Exports = run.Exports?.OrderByDescending(e => e.CreatedAt)
                    .Select(e => new PipelineRunExportResponseDTO
                    {
                        Id = e.Id,
                        FileName = e.FileName,
                        ContentType = e.ContentType,
                        SizeBytes = e.SizeBytes,
                        RowCount = e.RowCount,
                        CreatedAt = e.CreatedAt
                    }).ToList() ?? new List<PipelineRunExportResponseDTO>()
            };
        }

        public async Task<IEnumerable<PipelineResultResponseDTO>> GetResultsAsync(Guid id)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(
                id,
                "PipelineRunVideos,PipelineRunVideos.Video,PipelineRunVideos.PipelineResults,PipelineRunVideos.PipelineResults.AiModel");

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            return FlattenResults(run)
                .Select(MapResultToResponse)
                .OrderBy(r => r.AiModelName)
                .ThenBy(r => r.PipelineRunVideoId)
                .ToList();
        }

        public async Task<PagedResultDTO<PipelineRunHistoryResponseDTO>> GetHistoryAsync(
            string userId,
            int pageNumber = 1,
            int pageSize = 20,
            Guid? pipelineId = null,
            ExecutionStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            var ownerId = ParseUserId(userId);

            var (data, totalCount) = await _unitOfWork.Repository<PipelineRun>().GetPagedWithCountAsync(
                pageNumber,
                pageSize,
                r => r.StartedById == ownerId
                    && (pipelineId == null || r.PipelineId == pipelineId)
                    && (status == null || r.Status == status),
                q => q.OrderByDescending(r => r.StartedAt),
                HistoryIncludes,
                cancellationToken);

            return new PagedResultDTO<PipelineRunHistoryResponseDTO>
            {
                Data = data.Select(MapHistoryToResponse).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PipelineRunDetailResponseDTO> UpdateAsync(
            Guid id,
            UpdatePipelineRunDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(id, FullIncludes, cancellationToken);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            EnsureOwnership(run, userId);

            if (run.Status == ExecutionStatus.Running)
                throw new BadRequestException("Cannot edit a run while it is executing.");

            if (dto.PipelineId.HasValue && dto.PipelineId.Value != run.PipelineId)
            {
                var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(dto.PipelineId.Value, cancellationToken: cancellationToken);

                if (pipeline is null)
                    throw new NotFoundException($"Pipeline with ID {dto.PipelineId.Value} not found.");

                run.PipelineId = pipeline.Id;
            }

            var addedMedia = new List<Guid>();

            if (files is { Count: > 0 })
            {
                ValidateFiles(files);
                var saved = await SaveMediaAsync(files, cancellationToken);
                addedMedia = saved.Select(m => m.MediaId).ToList();
            }

            if (dto.RemoveMediaIds is { Count: > 0 })
            {
                var toRemove = run.PipelineRunVideos
                    .Where(v => dto.RemoveMediaIds.Contains(v.VideoId))
                    .ToList();

                foreach (var entity in toRemove)
                {
                    run.PipelineRunVideos.Remove(entity);
                    _unitOfWork.Repository<PipelineRunVideo>().Delete(entity);
                }
            }

            _unitOfWork.Repository<PipelineRun>().Update(run);

            if (addedMedia.Count > 0)
                await AttachRunContentAsync(run, addedMedia, new List<RunModelSelectionDTO>(), cancellationToken);

            if (dto.Models is { Count: > 0 })
            {
                var ordered = dto.Models.OrderBy(m => m.Order).ToList();
                var modelIds = ordered.Select(m => m.AiModelId).ToList();

                var found = await _unitOfWork.Repository<AiModel>()
                    .FindAllAsync(m => modelIds.Contains(m.Id), cancellationToken: cancellationToken);

                var foundIds = found.Select(m => m.Id).ToHashSet();
                var missing = modelIds.Where(m => !foundIds.Contains(m)).ToList();

                if (missing.Count > 0)
                    throw new NotFoundException($"AI Model not found: {string.Join(", ", missing)}");

                var existing = run.PipelineRunModels.ToList();

                foreach (var entity in existing)
                    _unitOfWork.Repository<PipelineRunModel>().Delete(entity);

                run.PipelineRunModels.Clear();

                foreach (var selection in ordered)
                {
                    run.PipelineRunModels.Add(new PipelineRunModel
                    {
                        PipelineRunId = run.Id,
                        AiModelId = selection.AiModelId,
                        Order = selection.Order,
                        ConfigurationJson = selection.ConfigurationJson
                    });
                }
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            return await GetDetailAsync(id);
        }

        public async Task<RunExecutionResponseDTO> ReRunAsync(Guid id, string userId, CancellationToken cancellationToken = default)
        {
            var source = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(id, FullIncludes, cancellationToken);

            if (source is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            EnsureOwnership(source, userId);

            if (source.Status == ExecutionStatus.Running)
                throw new BadRequestException("This run is currently executing.");

            var newRun = new PipelineRun
            {
                PipelineId = source.PipelineId,
                StartedById = ParseUserId(userId),
                Status = ExecutionStatus.Pending
            };

            await _unitOfWork.Repository<PipelineRun>().AddAsync(newRun, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var mediaIds = source.PipelineRunVideos.Select(v => v.VideoId).ToList();
            var models = source.PipelineRunModels
                .OrderBy(m => m.Order)
                .Select(m => new RunModelSelectionDTO
                {
                    AiModelId = m.AiModelId,
                    Order = m.Order,
                    ConfigurationJson = m.ConfigurationJson
                })
                .ToList();

            await AttachRunContentAsync(newRun, mediaIds, models, cancellationToken);

            var jobId = _executionQueue.QueueExecution(newRun.Id);

            return new RunExecutionResponseDTO
            {
                RunId = newRun.Id,
                JobId = jobId,
                Status = newRun.Status.ToString(),
                UploadedCount = mediaIds.Count,
                ModelCount = models.Count,
                Message = $"Re-run of {id} queued with the same media and models."
            };
        }

        public async Task<(byte[] Content, string FileName, string ContentType)> GetLatestExportAsync(Guid id)
        {
            var export = await _unitOfWork.Repository<PipelineRunExport>()
                .FindAsync(e => e.PipelineRunId == id, cancellationToken: default);

            if (export is null)
                throw new NotFoundException($"No Excel export found for run {id}.");

            return (export.Content, export.FileName, export.ContentType);
        }

        public async Task<PipelineRunResponseDTO> UpdateStatusAsync(Guid id, UpdatePipelineRunStatusDTO dto)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(id);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            run.Status = dto.Status;

            if (dto.Status == ExecutionStatus.Succeeded || dto.Status == ExecutionStatus.Failed)
                run.CompletedAt = DateTime.UtcNow;

            _unitOfWork.Repository<PipelineRun>().Update(run);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(run);
        }

        public async Task<PipelineRunVideoResponseDTO> AddVideoAsync(Guid runId, AddPipelineRunVideoDTO dto)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(runId);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {runId} not found.");

            var video = await _unitOfWork.Repository<Video>().GetByIdAsync(dto.VideoId);

            if (video is null)
                throw new NotFoundException($"Video with ID {dto.VideoId} not found.");

            var exists = await _unitOfWork.Repository<PipelineRunVideo>()
                .ExistsAsync(v => v.PipelineRunId == runId && v.VideoId == dto.VideoId);

            if (exists)
                throw new BadRequestException("This video has already been added to the run.");

            var runVideo = new PipelineRunVideo
            {
                PipelineRunId = runId,
                VideoId = dto.VideoId,
                Status = ExecutionStatus.Pending
            };

            await _unitOfWork.Repository<PipelineRunVideo>().AddAsync(runVideo);
            await _unitOfWork.CompleteAsync();

            return new PipelineRunVideoResponseDTO
            {
                Id = runVideo.Id,
                PipelineRunId = runVideo.PipelineRunId,
                VideoId = runVideo.VideoId,
                VideoFileName = video.FileName,
                Status = runVideo.Status.ToString(),
                ProcessedAt = runVideo.ProcessedAt,
                Notes = runVideo.Notes,
                CreatedAt = runVideo.CreatedAt
            };
        }

        public async Task<PipelineRunModelResponseDTO> AddModelAsync(Guid runId, AddPipelineRunModelDTO dto)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(runId);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {runId} not found.");

            var aiModel = await _unitOfWork.Repository<AiModel>().GetByIdAsync(dto.AiModelId);

            if (aiModel is null)
                throw new NotFoundException($"AI Model with ID {dto.AiModelId} not found.");

            var runModel = new PipelineRunModel
            {
                PipelineRunId = runId,
                AiModelId = dto.AiModelId,
                Order = dto.Order,
                ConfigurationJson = dto.ConfigurationJson
            };

            await _unitOfWork.Repository<PipelineRunModel>().AddAsync(runModel);
            await _unitOfWork.CompleteAsync();

            return new PipelineRunModelResponseDTO
            {
                Id = runModel.Id,
                PipelineRunId = runModel.PipelineRunId,
                AiModelId = runModel.AiModelId,
                AiModelName = aiModel.Name,
                Order = runModel.Order,
                ConfigurationJson = runModel.ConfigurationJson
            };
        }

        public async Task RemoveVideoAsync(Guid runId, Guid videoId)
        {
            var runVideo = await _unitOfWork.Repository<PipelineRunVideo>()
                .FindAsync(v => v.PipelineRunId == runId && v.VideoId == videoId);

            if (runVideo is null)
                throw new NotFoundException($"Video {videoId} not found in Run {runId}.");

            _unitOfWork.Repository<PipelineRunVideo>().Delete(runVideo);
            await _unitOfWork.CompleteAsync();
        }

        public async Task RemoveModelAsync(Guid runId, Guid modelId)
        {
            var runModel = await _unitOfWork.Repository<PipelineRunModel>()
                .FindAsync(m => m.PipelineRunId == runId && m.AiModelId == modelId);

            if (runModel is null)
                throw new NotFoundException($"AI Model {modelId} not found in Run {runId}.");

            _unitOfWork.Repository<PipelineRunModel>().Delete(runModel);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<PipelineRunResponseDTO>> GetAllByUserAsync(string userId)
        {
            var runs = await _unitOfWork.Repository<PipelineRun>()
                .FindAllAsync(
                    r => r.StartedById == ParseUserId(userId),
                    FullIncludes);

            return runs.Select(MapToResponse).ToList();
        }

        private static void ValidateFiles(IReadOnlyList<UploadedMediaInput> files)
        {
            var allowed = MediaKindHelper.ImageExtensions.Concat(MediaKindHelper.VideoExtensions);

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.FileName))
                    throw new BadRequestException("One of the uploaded files has no name.");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowed.Contains(extension))
                    throw new BadRequestException($"Unsupported file type '{extension}'. Allowed: {string.Join(", ", allowed)}");
            }
        }

        private async Task<List<UploadedMediaResultDTO>> SaveMediaAsync(
            IReadOnlyList<UploadedMediaInput> files,
            CancellationToken cancellationToken)
        {
            var videos = new List<Video>();

            foreach (var file in files)
            {
                var relativePath = await _mediaStorage.SaveAsync(file.Content, file.FileName, cancellationToken);

                videos.Add(new Video
                {
                    FileName = Path.GetFileName(file.FileName),
                    FilePath = relativePath,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length > 0 ? file.Length : null
                });
            }

            await _unitOfWork.Repository<Video>().AddRangeAsync(videos, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return videos.Select(v => new UploadedMediaResultDTO
            {
                MediaId = v.Id,
                FileName = v.FileName,
                MediaKind = MediaKindHelper.Detect(v.FileName, v.ContentType),
                SizeBytes = v.SizeBytes
            }).ToList();
        }

        private async Task AttachRunContentAsync(
            PipelineRun run,
            List<Guid> mediaIds,
            List<RunModelSelectionDTO> modelSelections,
            CancellationToken cancellationToken)
        {
            var runVideos = mediaIds.Select(mediaId => new PipelineRunVideo
            {
                PipelineRunId = run.Id,
                VideoId = mediaId,
                Status = ExecutionStatus.Pending
            }).ToList();

            var runModels = modelSelections.Select(selection => new PipelineRunModel
            {
                PipelineRunId = run.Id,
                AiModelId = selection.AiModelId,
                Order = selection.Order,
                ConfigurationJson = selection.ConfigurationJson
            }).ToList();

            await _unitOfWork.Repository<PipelineRunVideo>().AddRangeAsync(runVideos, cancellationToken);
            await _unitOfWork.Repository<PipelineRunModel>().AddRangeAsync(runModels, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }

        private static List<PipelineResult> FlattenResults(PipelineRun run)
        {
            return run.PipelineRunVideos?
                .SelectMany(v => v.PipelineResults ?? new List<PipelineResult>())
                .ToList() ?? new List<PipelineResult>();
        }

        private static double? AverageConfidence(IEnumerable<PipelineResult> results)
        {
            var values = results.Where(r => r.Confidence.HasValue).Select(r => r.Confidence!.Value).ToList();

            return values.Count == 0 ? null : values.Average();
        }

        private static void EnsureOwnership(PipelineRun run, string userId)
        {
            if (run.StartedById is null)
                return;

            if (run.StartedById.Value != ParseUserId(userId))
                throw new UnauthorizedException("You do not have access to this pipeline run.");
        }

        private static Guid? ParseUserId(string? userId)
        {
            return Guid.TryParse(userId, out var id) ? id : null;
        }

        private static PipelineRunResponseDTO MapToResponse(PipelineRun run)
        {
            return new PipelineRunResponseDTO
            {
                Id = run.Id,
                PipelineId = run.PipelineId,
                PipelineName = run.Pipeline?.Name ?? string.Empty,
                StartedById = run.StartedById,
                StartedByName = run.StartedBy?.FullName,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                Status = run.Status.ToString(),
                CreatedAt = run.CreatedAt,
                PipelineRunVideos = run.PipelineRunVideos?.Select(MapVideoToResponse).ToList()
                    ?? new List<PipelineRunVideoResponseDTO>(),
                PipelineRunModels = run.PipelineRunModels?.Select(MapModelToResponse).ToList()
                    ?? new List<PipelineRunModelResponseDTO>()
            };
        }

        private static PipelineRunHistoryResponseDTO MapHistoryToResponse(PipelineRun run)
        {
            var results = FlattenResults(run);

            return new PipelineRunHistoryResponseDTO
            {
                Id = run.Id,
                PipelineId = run.PipelineId,
                PipelineName = run.Pipeline?.Name ?? string.Empty,
                StartedByName = run.StartedBy?.FullName,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                Status = run.Status.ToString(),
                ErrorMessage = null,
                TotalMedia = run.PipelineRunVideos?.Count ?? 0,
                TotalModels = run.PipelineRunModels?.Count ?? 0,
                TotalResults = results.Count,
                AverageConfidence = AverageConfidence(results),
                HasExport = run.Exports is { Count: > 0 },
                DurationSeconds = run.CompletedAt.HasValue
                    ? (int)Math.Round((run.CompletedAt.Value - run.StartedAt).TotalSeconds)
                    : 0
            };
        }

        private static PipelineResultResponseDTO MapResultToResponse(PipelineResult r)
        {
            return new PipelineResultResponseDTO
            {
                Id = r.Id,
                PipelineRunVideoId = r.PipelineRunVideoId,
                AiModelId = r.AiModelId,
                AiModelName = r.AiModel?.Name ?? string.Empty,
                ResultJson = r.ResultJson,
                Confidence = r.Confidence,
                ResultType = r.ResultType,
                ProcessedAt = r.ProcessedAt,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            };
        }

        private static PipelineRunVideoResponseDTO MapVideoToResponse(PipelineRunVideo v)
        {
            return new PipelineRunVideoResponseDTO
            {
                Id = v.Id,
                PipelineRunId = v.PipelineRunId,
                VideoId = v.VideoId,
                VideoFileName = v.Video?.FileName ?? string.Empty,
                Status = v.Status.ToString(),
                ProcessedAt = v.ProcessedAt,
                Notes = v.Notes,
                CreatedAt = v.CreatedAt,
                PipelineResults = v.PipelineResults?.Select(MapResultToResponse).ToList()
                    ?? new List<PipelineResultResponseDTO>()
            };
        }

        private static PipelineRunModelResponseDTO MapModelToResponse(PipelineRunModel m)
        {
            return new PipelineRunModelResponseDTO
            {
                Id = m.Id,
                PipelineRunId = m.PipelineRunId,
                AiModelId = m.AiModelId,
                AiModelName = m.AiModel?.Name ?? string.Empty,
                Order = m.Order,
                ConfigurationJson = m.ConfigurationJson
            };
        }
    }
}
