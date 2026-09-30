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
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Application.VisionDetection.Models;
using VisionAiChrono.Domain.Enumration;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class PipelineRunService : IPipelineRunService
    {
        private const string FullIncludes =
            "Pipeline,StartedBy,PipelineRunModels," +
            "PipelineRunVideos,PipelineRunVideos.Video," +
            "PipelineRunVideos.PipelineResults,Exports";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPipelineExecutionService _executionService;
        private readonly IPipelineExecutionQueue _executionQueue;
        private readonly IMediaStorageService _mediaStorage;

        public PipelineRunService(
            IUnitOfWork unitOfWork,
            IPipelineExecutionService executionService,
            IPipelineExecutionQueue executionQueue,
            IMediaStorageService mediaStorage)
        {
            _unitOfWork = unitOfWork;
            _executionService = executionService;
            _executionQueue = executionQueue;
            _mediaStorage = mediaStorage;
        }

        public async Task<RunExecutionResponseDTO> ExecuteAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var (run, tasks, savedMedia) = await PrepareRunAsync(dto, files, userId, cancellationToken);

            // The run is still tracked from AddAsync. Detach it so the execution
            // service can read and attach its own copy without an identity conflict.
            _unitOfWork.Repository<PipelineRun>().Detach(run);

            var summary = await _executionService.ExecuteRunAsync(run.Id, cancellationToken);

            return new RunExecutionResponseDTO
            {
                RunId = run.Id,
                Status = summary.Status,
                UploadedCount = savedMedia.Count,
                TaskCount = tasks.Count,
                TotalExecutions = summary.TotalExecutions,
                Succeeded = summary.Succeeded,
                Failed = summary.Failed,
                TotalDetections = summary.TotalDetections,
                AverageConfidence = summary.AverageConfidence,
                HasExport = summary.HasExport,
                ExportFileName = summary.ExportFileName,
                CompletedAt = summary.CompletedAt,
                Message = $"Ran {summary.TotalExecutions} detection(s): {summary.Succeeded} succeeded, " +
                          $"{summary.Failed} failed. Found {summary.TotalDetections} object(s).",
                UploadedMedia = savedMedia,
                Results = summary.Results
            };
        }

        public async Task<RunExecutionResponseDTO> QueueExecutionAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var (run, tasks, savedMedia) = await PrepareRunAsync(dto, files, userId, cancellationToken);

            var jobId = _executionQueue.QueueExecution(run.Id);

            return new RunExecutionResponseDTO
            {
                RunId = run.Id,
                JobId = jobId,
                Status = run.Status.ToString(),
                UploadedCount = savedMedia.Count,
                TaskCount = tasks.Count,
                Message = $"Queued {savedMedia.Count} media item(s) across {tasks.Count} task(s) " +
                          $"= {savedMedia.Count * tasks.Count} detection(s). Poll " +
                          $"/api/PipelineRun/{run.Id}/results for the detections.",
                UploadedMedia = savedMedia
            };
        }

        private async Task<(PipelineRun Run, List<DetectionTask> Tasks, List<UploadedMediaResultDTO> Media)> PrepareRunAsync(
            CreateRunWithMediaDTO dto,
            IReadOnlyList<UploadedMediaInput> files,
            string userId,
            CancellationToken cancellationToken)
        {
            if (files is null || files.Count == 0)
                throw new BadRequestException("At least one video or image file is required.");

            if (dto.Tasks is null || dto.Tasks.Count == 0)
                throw new BadRequestException("At least one task is required.");

            var tasks = ValidateTasks(dto.Tasks);
            ValidateFiles(files);

            var pipeline = await _unitOfWork.Repository<Pipeline>()
                .GetByIdAsync(dto.PipelineId, cancellationToken: cancellationToken);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {dto.PipelineId} not found.");

            var savedMedia = await SaveMediaAsync(files, cancellationToken);

            var run = new PipelineRun
            {
                PipelineId = dto.PipelineId,
                StartedById = ParseUserId(userId),
                Status = ExecutionStatus.Pending
            };

            await _unitOfWork.Repository<PipelineRun>().AddAsync(run, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await AttachRunContentAsync(run, savedMedia.Select(m => m.MediaId).ToList(), tasks, cancellationToken);

            return (run, tasks, savedMedia);
        }

        public async Task<PipelineRunDetailResponseDTO> GetDetailAsync(Guid id)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(id, FullIncludes);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            var results = FlattenResults(run);
            var confidences = results.Where(r => r.Confidence.HasValue).Select(r => r.Confidence!.Value).ToList();

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
                CreatedAt = run.CreatedAt,
                TotalMedia = run.PipelineRunVideos?.Count ?? 0,
                TotalModels = run.PipelineRunModels?.Count ?? 0,
                TotalResults = results.Count,
                AverageConfidence = AverageConfidence(results),
                PipelineRunVideos = run.PipelineRunVideos?.Select(MapVideoToResponse).ToList()
                    ?? new List<PipelineRunVideoResponseDTO>(),
                PipelineRunModels = run.PipelineRunModels?.Select(MapModelToResponse).ToList()
                    ?? new List<PipelineRunModelResponseDTO>(),
                Exports = run.Exports?.Select(e => new PipelineRunExportResponseDTO
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
                "PipelineRunVideos,PipelineRunVideos.Video,PipelineRunVideos.PipelineResults");

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {id} not found.");

            return FlattenResults(run)
                .Select(MapResultToResponse)
                .OrderBy(r => r.TaskIndex)
                .ThenBy(r => r.PipelineRunVideoId)
                .ToList();
        }

        public async Task<(byte[] Content, string FileName, string ContentType)> GetLatestExportAsync(Guid id)
        {
            var export = await _unitOfWork.Repository<PipelineRunExport>()
                .FindAsync(e => e.PipelineRunId == id, cancellationToken: default);

            if (export is null)
                throw new NotFoundException($"No Excel export found for run {id}.");

            return (export.Content, export.FileName, export.ContentType);
        }

        private static List<DetectionTask> ValidateTasks(IReadOnlyList<int> tasks)
        {
            var resolved = new List<DetectionTask>();
            var seen = new HashSet<int>();
            var invalid = new List<int>();

            foreach (var index in tasks)
            {
                var task = DetectionTaskExtensions.FromIndex(index);

                if (task is null)
                {
                    invalid.Add(index);
                    continue;
                }

                if (seen.Add(index))
                    resolved.Add(task.Value);
            }

            if (invalid.Count > 0)
                throw new BadRequestException(
                    $"Unsupported task(s): {string.Join(", ", invalid)}. Use 0 (person), 1 (weapon) or 2 (fire).");

            if (resolved.Count == 0)
                throw new BadRequestException("At least one task is required.");

            return resolved;
        }

        private static void ValidateFiles(IReadOnlyList<UploadedMediaInput> files)
        {
            if (files is null || files.Count == 0)
                throw new BadRequestException("At least one video or image file is required.");

            var supported = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;

                if (!supported.Contains(extension))
                {
                    throw new BadRequestException(
                        $"Unsupported file type '{extension}'. Supported: {string.Join(", ", supported)}");
                }

                if (file.Length <= 0)
                    throw new BadRequestException($"File '{file.FileName}' is empty.");
            }
        }

        private async Task<List<UploadedMediaResultDTO>> SaveMediaAsync(
            IReadOnlyList<UploadedMediaInput> files,
            CancellationToken cancellationToken)
        {
            var saved = new List<UploadedMediaResultDTO>(files.Count);

            foreach (var file in files)
            {
                var storedPath = await _mediaStorage.SaveAsync(
                    file.Content, file.FileName, cancellationToken);

                var mediaKind = MediaKindHelper.Detect(file.FileName, file.ContentType);

                var video = new Video
                {
                    FileName = file.FileName,
                    FilePath = storedPath,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length
                };

                await _unitOfWork.Repository<Video>().AddAsync(video, cancellationToken);
                await _unitOfWork.CompleteAsync(cancellationToken);

                saved.Add(new UploadedMediaResultDTO
                {
                    MediaId = video.Id,
                    FileName = file.FileName,
                    SizeBytes = file.Length,
                    MediaKind = mediaKind
                });
            }

            return saved;
        }

        private async Task AttachRunContentAsync(
            PipelineRun run,
            List<Guid> mediaIds,
            List<DetectionTask> tasks,
            CancellationToken cancellationToken)
        {
            foreach (var mediaId in mediaIds)
            {
                await _unitOfWork.Repository<PipelineRunVideo>().AddAsync(new PipelineRunVideo
                {
                    PipelineRunId = run.Id,
                    VideoId = mediaId,
                    Status = ExecutionStatus.Pending
                }, cancellationToken);
            }

            var order = 0;

            foreach (var task in tasks)
            {
                await _unitOfWork.Repository<PipelineRunModel>().AddAsync(new PipelineRunModel
                {
                    PipelineRunId = run.Id,
                    TaskIndex = task.ToIndex(),
                    Order = order++
                }, cancellationToken);
            }

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

        private static Guid? ParseUserId(string? userId)
        {
            return Guid.TryParse(userId, out var id) ? id : null;
        }

        private static string ResolveTaskName(int? taskIndex)
        {
            if (taskIndex is null)
                return string.Empty;

            var task = DetectionTaskExtensions.FromIndex(taskIndex.Value);

            return task?.ToString() ?? string.Empty;
        }

        private static PipelineResultResponseDTO MapResultToResponse(PipelineResult r)
        {
            return new PipelineResultResponseDTO
            {
                Id = r.Id,
                PipelineRunVideoId = r.PipelineRunVideoId,
                AiModelId = r.AiModelId,
                AiModelName = r.AiModel?.Name ?? string.Empty,
                TaskIndex = r.TaskIndex,
                TaskName = r.AiModel?.Name ?? ResolveTaskName(r.TaskIndex),
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
                TaskIndex = m.TaskIndex,
                TaskName = m.AiModel?.Name ?? ResolveTaskName(m.TaskIndex),
                Order = m.Order,
                ConfigurationJson = m.ConfigurationJson
            };
        }
    }
}
