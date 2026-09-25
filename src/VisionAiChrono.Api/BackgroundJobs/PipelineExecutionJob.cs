using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using Hangfire;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Application.VisionDetection.Models;
using VisionAiChrono.Domain.Enumration;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Api.BackgroundJobs
{
    public class PipelineExecutionJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVisionDetectionService _visionDetectionService;
        private readonly IMediaStorageService _mediaStorage;
        private readonly IExcelExportService _excelExport;
        private readonly ILogger<PipelineExecutionJob> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public PipelineExecutionJob(
            IUnitOfWork unitOfWork,
            IVisionDetectionService visionDetectionService,
            IMediaStorageService mediaStorage,
            IExcelExportService excelExport,
            ILogger<PipelineExecutionJob> logger)
        {
            _unitOfWork = unitOfWork;
            _visionDetectionService = visionDetectionService;
            _mediaStorage = mediaStorage;
            _excelExport = excelExport;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAsync(Guid pipelineRunId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pipeline execution job started for run {RunId}", pipelineRunId);

            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(
                pipelineRunId,
                "Pipeline,PipelineRunModels,PipelineRunModels.AiModel," +
                "PipelineRunVideos,PipelineRunVideos.Video",
                cancellationToken);

            if (run is null)
            {
                _logger.LogWarning("Pipeline run {RunId} no longer exists. Skipping.", pipelineRunId);
                return;
            }

            if (run.Status == ExecutionStatus.Succeeded)
            {
                _logger.LogInformation("Pipeline run {RunId} already succeeded. Skipping.", pipelineRunId);
                return;
            }

            run.Status = ExecutionStatus.Running;
            run.CompletedAt = null;
            _unitOfWork.Repository<PipelineRun>().Update(run);
            await _unitOfWork.CompleteAsync(cancellationToken);

            try
            {
                await RunAllAsync(run, cancellationToken);

                run.Status = ExecutionStatus.Succeeded;
                run.CompletedAt = DateTime.UtcNow;
                _unitOfWork.Repository<PipelineRun>().Update(run);
                await _unitOfWork.CompleteAsync(cancellationToken);

                await BuildExportAsync(run, cancellationToken);

                _logger.LogInformation("Pipeline run {RunId} completed successfully.", pipelineRunId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline run {RunId} failed.", pipelineRunId);

                var failed = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(pipelineRunId, cancellationToken: cancellationToken);

                if (failed is not null)
                {
                    failed.Status = ExecutionStatus.Failed;
                    failed.CompletedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<PipelineRun>().Update(failed);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }

                throw;
            }
        }

        private async Task RunAllAsync(PipelineRun run, CancellationToken cancellationToken)
        {
            var models = (run.PipelineRunModels ?? new List<PipelineRunModel>())
                .OrderBy(m => m.Order)
                .ToList();

            if (models.Count == 0)
                throw new InvalidOperationException("The run has no models selected.");

            var runVideos = run.PipelineRunVideos?.ToList() ?? new List<PipelineRunVideo>();

            if (runVideos.Count == 0)
                throw new InvalidOperationException("The run has no videos or images selected.");

            foreach (var runVideo in runVideos)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);

                await ProcessMediaAsync(run, runVideo, models, cancellationToken);
            }
        }

        private async Task ProcessMediaAsync(
            PipelineRun run,
            PipelineRunVideo runVideo,
            List<PipelineRunModel> models,
            CancellationToken cancellationToken)
        {
            var media = runVideo.Video;

            if (media is null)
            {
                await AddResultAsync(runVideo, null, null, null, null, "video", null, ExecutionStatus.Failed, cancellationToken);
                return;
            }

            if (!_mediaStorage.Exists(media.FilePath))
            {
                _logger.LogWarning("Media file missing on disk: {Path}", media.FilePath);
                runVideo.Notes = "File missing on disk";
                _unitOfWork.Repository<PipelineRunVideo>().Update(runVideo);
                await _unitOfWork.CompleteAsync(cancellationToken);

                await AddResultAsync(runVideo, null, null, null, null, "missing", null, ExecutionStatus.Failed, cancellationToken);
                return;
            }

            var absolutePath = _mediaStorage.GetAbsolutePath(media.FilePath);

            foreach (var runModel in models)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);

                var aiModel = runModel.AiModel;
                var task = DetectionTaskResolver.Resolve(aiModel);
                var modelName = aiModel?.Name ?? "unknown";

                _logger.LogInformation(
                    "Processing {File} with model {Model} (task {Task})",
                    media.FileName, modelName, task);

                try
                {
                    await using var stream = new FileStream(
                        absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

                    var response = await _visionDetectionService.DetectAsync(
                        task, stream, media.FileName, cancellationToken);

                    await AddResultAsync(
                        runVideo,
                        aiModel?.Id,
                        JsonSerializer.Serialize(response, JsonOptions),
                        response.Type,
                        ResolveConfidence(response),
                        task.ToString(),
                        response,
                        ExecutionStatus.Succeeded,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Model {Model} failed on {File}", modelName, media.FileName);

                    await AddResultAsync(
                        runVideo,
                        aiModel?.Id,
                        JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions),
                        "error",
                        null,
                        task.ToString(),
                        null,
                        ExecutionStatus.Failed,
                        cancellationToken);
                }
            }

            runVideo.Status = ExecutionStatus.Succeeded;
            runVideo.ProcessedAt = DateTime.UtcNow;
            _unitOfWork.Repository<PipelineRunVideo>().Update(runVideo);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }

        private Task AddResultAsync(
            PipelineRunVideo runVideo,
            Guid? aiModelId,
            string? resultJson,
            string? resultType,
            double? confidence,
            string? taskName,
            DetectionResponse? response,
            ExecutionStatus status,
            CancellationToken cancellationToken)
        {
            var result = new PipelineResult
            {
                PipelineRunVideoId = runVideo.Id,
                AiModelId = aiModelId ?? Guid.Empty,
                ResultJson = resultJson ?? "{}",
                ResultType = resultType,
                Confidence = confidence,
                Status = status,
                ProcessedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<PipelineResult>().AddAsync(result, cancellationToken);
            _unitOfWork.Repository<PipelineRunVideo>().Update(runVideo);

            return _unitOfWork.CompleteAsync(cancellationToken);
        }

        private async Task BuildExportAsync(PipelineRun run, CancellationToken cancellationToken)
        {
            try
            {
                var (content, fileName) = await _excelExport.BuildRunResultsAsync(run.Id, cancellationToken);

                var export = new PipelineRunExport
                {
                    PipelineRunId = run.Id,
                    FileName = fileName,
                    Content = content,
                    SizeBytes = content.LongLength,
                    RowCount = run.PipelineRunVideos?
                        .Sum(v => v.PipelineResults?.Count ?? 0) ?? 0
                };

                await _unitOfWork.Repository<PipelineRunExport>().AddAsync(export, cancellationToken);
                await _unitOfWork.CompleteAsync(cancellationToken);

                _logger.LogInformation("Excel export built for run {RunId} ({Size} bytes)", run.Id, content.LongLength);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build Excel export for run {RunId}. Run itself succeeded.", run.Id);
            }
        }

        private static double? ResolveConfidence(DetectionResponse response)
        {
            if (response is null)
                return null;

            if (response.Detections is { Count: > 0 })
            {
                return response.Detections.Average(d => d.Confidence);
            }

            if (response.Detected.HasValue && !response.Detected.Value)
                return 0d;

            return null;
        }
    }
}
