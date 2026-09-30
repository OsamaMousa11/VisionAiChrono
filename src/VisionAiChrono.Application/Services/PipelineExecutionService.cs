using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Application.VisionDetection.Models;
using VisionAiChrono.Domain.Enumration;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class PipelineExecutionService : IPipelineExecutionService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PipelineExecutionService> _logger;

        public PipelineExecutionService(
            IServiceScopeFactory scopeFactory,
            ILogger<PipelineExecutionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task<PipelineExecutionSummaryDTO> ExecuteRunAsync(
            Guid pipelineRunId, CancellationToken cancellationToken = default)
        {
            // Run inside a dedicated DI scope so it gets its own DbContext. This keeps
            // the execution isolated from anything the caller already tracked, which
            // otherwise causes EF identity conflicts on PipelineRun / Video rows.
            var scope = _scopeFactory.CreateScope();

            return ExecuteInScopeAsync(scope.ServiceProvider, pipelineRunId, cancellationToken);
        }

        private async Task<PipelineExecutionSummaryDTO> ExecuteInScopeAsync(
            IServiceProvider provider,
            Guid pipelineRunId,
            CancellationToken cancellationToken)
        {
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var visionDetectionService = provider.GetRequiredService<IVisionDetectionService>();
            var mediaStorage = provider.GetRequiredService<IMediaStorageService>();
            var excelExport = provider.GetRequiredService<IExcelExportService>();

            var context = new ExecutionContext(
                unitOfWork, visionDetectionService, mediaStorage, excelExport, _logger);

            var run = await unitOfWork.Repository<PipelineRun>().GetByIdAsync(
                pipelineRunId,
                "Pipeline,PipelineRunModels," +
                "PipelineRunVideos,PipelineRunVideos.Video",
                cancellationToken);

            if (run is null)
                throw new InvalidOperationException($"Pipeline run {pipelineRunId} no longer exists.");

            if (run.Status == ExecutionStatus.Succeeded)
                return await context.BuildSummaryAsync(run, null, null, cancellationToken);

            run.Status = ExecutionStatus.Running;
            run.CompletedAt = null;
            unitOfWork.Repository<PipelineRun>().Update(run);
            await unitOfWork.CompleteAsync(cancellationToken);

            try
            {
                var outcomes = await context.RunAllAsync(run, cancellationToken);

                run.Status = ExecutionStatus.Succeeded;
                run.CompletedAt = DateTime.UtcNow;
                unitOfWork.Repository<PipelineRun>().Update(run);
                await unitOfWork.CompleteAsync(cancellationToken);

                var exportFileName = await context.BuildExportAsync(run, cancellationToken);

                return await context.BuildSummaryAsync(run, outcomes, exportFileName, cancellationToken);
            }
            catch
            {
                var failed = await unitOfWork.Repository<PipelineRun>()
                    .GetByIdAsync(pipelineRunId, cancellationToken: cancellationToken);

                if (failed is not null)
                {
                    failed.Status = ExecutionStatus.Failed;
                    failed.CompletedAt = DateTime.UtcNow;
                    unitOfWork.Repository<PipelineRun>().Update(failed);
                    await unitOfWork.CompleteAsync(cancellationToken);
                }

                throw;
            }
        }

        /// <summary>
        /// Holds the per-scope dependencies used while executing one run.
        /// </summary>
        private sealed class ExecutionContext
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IVisionDetectionService _visionDetectionService;
            private readonly IMediaStorageService _mediaStorage;
            private readonly IExcelExportService _excelExport;
            private readonly ILogger _logger;

            public ExecutionContext(
                IUnitOfWork unitOfWork,
                IVisionDetectionService visionDetectionService,
                IMediaStorageService mediaStorage,
                IExcelExportService excelExport,
                ILogger logger)
            {
                _unitOfWork = unitOfWork;
                _visionDetectionService = visionDetectionService;
                _mediaStorage = mediaStorage;
                _excelExport = excelExport;
                _logger = logger;
            }

            internal async Task<List<DetectionOutcomeDTO>> RunAllAsync(PipelineRun run, CancellationToken cancellationToken)
            {
                var tasks = (run.PipelineRunModels ?? new List<PipelineRunModel>())
                    .OrderBy(m => m.Order)
                    .ToList();

                if (tasks.Count == 0)
                    throw new InvalidOperationException("The run has no tasks selected.");

                var media = run.PipelineRunVideos?.ToList() ?? new List<PipelineRunVideo>();

                if (media.Count == 0)
                    throw new InvalidOperationException("The run has no videos or images selected.");

                var outcomes = new List<DetectionOutcomeDTO>();

                foreach (var runVideo in media)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    outcomes.AddRange(await ProcessMediaAsync(runVideo, tasks, cancellationToken));
                }

                return outcomes;
            }

            internal async Task<List<DetectionOutcomeDTO>> ProcessMediaAsync(
                PipelineRunVideo runVideo,
                List<PipelineRunModel> tasks,
                CancellationToken cancellationToken)
            {
                var outcomes = new List<DetectionOutcomeDTO>();
                var media = runVideo.Video;

                if (media is null || !_mediaStorage.Exists(media.FilePath))
                {
                    var reason = media is null ? "Media record missing" : "File missing on disk";

                    _logger.LogWarning("Skipping media {Id}: {Reason}", runVideo.Id, reason);

                    runVideo.Notes = reason;
                    _unitOfWork.Repository<PipelineRunVideo>().Update(runVideo);

                    outcomes.Add(await AddResultAsync(
                        runVideo, 0, null, "missing", null, null, "error", reason, cancellationToken));

                    return outcomes;
                }

                var absolutePath = _mediaStorage.GetAbsolutePath(media.FilePath);

                foreach (var runModel in tasks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var task = DetectionTaskExtensions.FromIndex(runModel.TaskIndex) ?? DetectionTask.Person;

                    try
                    {
                        await using var stream = new FileStream(
                            absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

                        var response = await _visionDetectionService.DetectAsync(
                            task, stream, media.FileName, cancellationToken);

                        outcomes.Add(await AddResultAsync(
                            runVideo,
                            runModel.TaskIndex,
                            JsonSerializer.Serialize(response, JsonOptions),
                            response.Type,
                            ResolveConfidence(response),
                            ReadDetectionCount(response),
                            "succeeded",
                            null,
                            cancellationToken));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Task {Task} failed on {File}", task, media.FileName);

                        outcomes.Add(await AddResultAsync(
                            runVideo,
                            runModel.TaskIndex,
                            JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions),
                            "error",
                            null,
                            0,
                            "failed",
                            ex.Message,
                            cancellationToken));
                    }
                }

                runVideo.Status = ExecutionStatus.Succeeded;
                runVideo.ProcessedAt = DateTime.UtcNow;
                _unitOfWork.Repository<PipelineRunVideo>().Update(runVideo);
                await _unitOfWork.CompleteAsync(cancellationToken);

                return outcomes;
            }

            internal async Task<DetectionOutcomeDTO> AddResultAsync(
                PipelineRunVideo runVideo,
                int taskIndex,
                string? resultJson,
                string? resultType,
                double? confidence,
                int? detections,
                string status,
                string? error,
                CancellationToken cancellationToken)
            {
                var result = new PipelineResult
                {
                    PipelineRunVideoId = runVideo.Id,
                    TaskIndex = taskIndex,
                    ResultJson = resultJson ?? "{}",
                    ResultType = resultType,
                    Confidence = confidence,
                    Status = status == "succeeded" ? ExecutionStatus.Succeeded : ExecutionStatus.Failed,
                    ProcessedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<PipelineResult>().AddAsync(result, cancellationToken);
                _unitOfWork.Repository<PipelineRunVideo>().Update(runVideo);
                await _unitOfWork.CompleteAsync(cancellationToken);

                var task = DetectionTaskExtensions.FromIndex(taskIndex);

                return new DetectionOutcomeDTO
                {
                    MediaFile = runVideo.Video?.FileName ?? string.Empty,
                    TaskIndex = taskIndex,
                    Task = task?.ToString() ?? string.Empty,
                    ResultType = resultType,
                    Detections = detections ?? 0,
                    Confidence = confidence,
                    Status = status,
                    Error = error,
                    ProcessedAt = result.ProcessedAt,
                    ResultJson = result.ResultJson
                };
            }

            internal async Task<string?> BuildExportAsync(PipelineRun run, CancellationToken cancellationToken)
            {
                try
                {
                    var (content, fileName) = await _excelExport.BuildRunResultsAsync(run.Id, cancellationToken);

                    await _unitOfWork.Repository<PipelineRunExport>().AddAsync(new PipelineRunExport
                    {
                        PipelineRunId = run.Id,
                        FileName = fileName,
                        Content = content,
                        SizeBytes = content.LongLength,
                        RowCount = run.PipelineRunVideos?.Sum(v => v.PipelineResults?.Count ?? 0) ?? 0
                    }, cancellationToken);

                    await _unitOfWork.CompleteAsync(cancellationToken);

                    _logger.LogInformation("Excel export built for run {RunId}", run.Id);

                    return fileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build Excel export for run {RunId}.", run.Id);
                    return null;
                }
            }

            internal async Task<PipelineExecutionSummaryDTO> BuildSummaryAsync(
                PipelineRun run,
                List<DetectionOutcomeDTO>? outcomes,
                string? exportFileName,
                CancellationToken cancellationToken)
            {
                if (outcomes is null)
                {
                    var persisted = await _unitOfWork.Repository<PipelineRun>()
                        .GetByIdAsync(run.Id, "PipelineRunVideos,PipelineRunVideos.Video,PipelineRunVideos.PipelineResults,Exports", cancellationToken);

                    outcomes = (persisted?.PipelineRunVideos ?? new List<PipelineRunVideo>())
                        .SelectMany(v => (v.PipelineResults ?? new List<PipelineResult>()).Select(r => new DetectionOutcomeDTO
                        {
                            MediaFile = v.Video?.FileName ?? string.Empty,
                            TaskIndex = r.TaskIndex ?? 0,
                            Task = DetectionTaskExtensions.FromIndex(r.TaskIndex ?? 0)?.ToString() ?? string.Empty,
                            ResultType = r.ResultType,
                            Detections = ReadDetectionCount(r.ResultJson),
                            Confidence = r.Confidence,
                            Status = r.Status.ToString().ToLowerInvariant(),
                            ProcessedAt = r.ProcessedAt,
                            ResultJson = r.ResultJson
                        }))
                        .ToList();

                    exportFileName = persisted?.Exports?.FirstOrDefault()?.FileName;
                }

                var confidences = outcomes.Where(o => o.Confidence.HasValue).Select(o => o.Confidence!.Value).ToList();

                return new PipelineExecutionSummaryDTO
                {
                    RunId = run.Id,
                    Status = run.Status.ToString(),
                    TotalExecutions = outcomes.Count,
                    Succeeded = outcomes.Count(o => o.Status == "succeeded"),
                    Failed = outcomes.Count(o => o.Status != "succeeded"),
                    TotalDetections = outcomes.Sum(o => o.Detections),
                    AverageConfidence = confidences.Count == 0 ? null : confidences.Average(),
                    HasExport = exportFileName is not null,
                    ExportFileName = exportFileName,
                    CompletedAt = run.CompletedAt,
                    Results = outcomes
                };
            }

            private static double? ResolveConfidence(DetectionResponse response)
            {
                if (response.Detections is { Count: > 0 })
                    return response.Detections.Average(d => d.Confidence);

                if (response.Detected.HasValue && !response.Detected.Value)
                    return 0d;

                return null;
            }

            private static int ReadDetectionCount(DetectionResponse? response)
            {
                if (response is null)
                    return 0;

                if (response.Count.HasValue)
                    return response.Count.Value;

                if (response.Detections is { Count: > 0 })
                    return response.Detections.Count;

                if (response.TotalDetections.HasValue)
                    return response.TotalDetections.Value;

                return 0;
            }

            private static int ReadDetectionCount(string? resultJson)
            {
                if (string.IsNullOrWhiteSpace(resultJson))
                    return 0;

                try
                {
                    using var document = JsonDocument.Parse(resultJson);
                    var root = document.RootElement;

                    if (root.TryGetProperty("count", out var count) && count.ValueKind == JsonValueKind.Number)
                        return count.GetInt32();

                    if (root.TryGetProperty("totalDetections", out var total) && total.ValueKind == JsonValueKind.Number)
                        return total.GetInt32();

                    if (root.TryGetProperty("detections", out var detections) && detections.ValueKind == JsonValueKind.Array)
                        return detections.GetArrayLength();
                }
                catch (JsonException)
                {
                }

                return 0;
            }
    }
    }
}
