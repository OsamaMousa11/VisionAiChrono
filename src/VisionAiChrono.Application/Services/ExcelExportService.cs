using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private const string ResultJsonColumn = "ResultJson";

        private readonly IUnitOfWork _unitOfWork;

        public ExcelExportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(byte[] Content, string FileName)> BuildRunResultsAsync(
            Guid pipelineRunId,
            CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.Repository<PipelineRun>().GetByIdAsync(
                pipelineRunId,
                "Pipeline,PipelineRunModels,PipelineRunModels.AiModel," +
                "PipelineRunVideos,PipelineRunVideos.Video," +
                "PipelineRunVideos.PipelineResults,PipelineRunVideos.PipelineResults.AiModel",
                cancellationToken);

            if (run is null)
                throw new NotFoundException($"Pipeline Run with ID {pipelineRunId} not found.");

            var results = run.PipelineRunVideos?
                .SelectMany(v => v.PipelineResults ?? new List<PipelineResult>())
                .ToList() ?? new List<PipelineResult>();

            using var workbook = new XLWorkbook();

            BuildSummarySheet(workbook, run, results);
            BuildResultsSheet(workbook, run, results);
            BuildMediaSheet(workbook, run);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return (stream.ToArray(), $"pipeline-run-{run.Id:yyyyMMdd-HHmmss}.xlsx");
        }

        private static void BuildSummarySheet(XLWorkbook workbook, PipelineRun run, List<PipelineResult> results)
        {
            var sheet = workbook.Worksheets.Add("Summary");
            var row = 1;

            void WriteSection(string title)
            {
                row++;
                var titleCell = sheet.Cell(row, 1);
                titleCell.Value = title;
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 12;
                titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
                titleCell.Style.Font.FontColor = XLColor.White;
                sheet.Range(row, 1, row, 2).Merge();
            }

            void WritePair(string label, object? value)
            {
                row++;
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 1).Style.Font.Bold = true;
                sheet.Cell(row, 2).Value = value is null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            var header = sheet.Cell(1, 1);
            header.Value = "Pipeline Run Report";
            header.Style.Font.Bold = true;
            header.Style.Font.FontSize = 16;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F2A4A");
            sheet.Range(1, 1, 1, 2).Merge();

            WriteSection("Run Information");
            WritePair("Run ID", run.Id);
            WritePair("Pipeline", run.Pipeline?.Name ?? string.Empty);
            WritePair("Status", run.Status.ToString());
            WritePair("Started By", run.StartedBy?.FullName ?? run.StartedBy?.Email ?? string.Empty);
            WritePair("Started At", run.StartedAt);
            WritePair("Completed At", run.CompletedAt);
            WritePair("Duration (seconds)", run.CompletedAt.HasValue
                ? Math.Round((run.CompletedAt.Value - run.StartedAt).TotalSeconds)
                : (object)"still running");

            WriteSection("Totals");
            WritePair("Media count", run.PipelineRunVideos?.Count ?? 0);
            WritePair("Model count", run.PipelineRunModels?.Count ?? 0);
            WritePair("Result count", results.Count);
            WritePair("Succeeded results", results.Count(r => r.Status == VisionAiChrono.Domain.Enumration.ExecutionStatus.Succeeded));
            WritePair("Failed results", results.Count(r => r.Status == VisionAiChrono.Domain.Enumration.ExecutionStatus.Failed));
            WritePair("Average confidence", results.Where(r => r.Confidence.HasValue).Select(r => r.Confidence!.Value).DefaultIfEmpty(0).Average());
            WritePair("Total detections", results.Sum(r => ReadDetectionCount(r)));

            sheet.Column(1).Width = 26;
            sheet.Column(2).Width = 46;
        }

        private static void BuildResultsSheet(XLWorkbook workbook, PipelineRun run, List<PipelineResult> results)
        {
            var sheet = workbook.Worksheets.Add("Results");

            var headers = new[]
            {
                "RunId", "Pipeline", "MediaFile", "Model", "ModelType", "Task",
                "ResultType", "Detections", "Confidence", "Status", "ProcessedAt", "ResultJson"
            };

            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = sheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var row = 2;

            foreach (var runVideo in run.PipelineRunVideos ?? new List<PipelineRunVideo>())
            {
                foreach (var result in runVideo.PipelineResults ?? new List<PipelineResult>())
                {
                    var model = result.AiModel;

                    sheet.Cell(row, 1).Value = run.Id.ToString();
                    sheet.Cell(row, 2).Value = run.Pipeline?.Name ?? string.Empty;
                    sheet.Cell(row, 3).Value = runVideo.Video?.FileName ?? string.Empty;
                    sheet.Cell(row, 4).Value = model?.Name ?? string.Empty;
                    sheet.Cell(row, 5).Value = model?.ModelType ?? string.Empty;
                    sheet.Cell(row, 6).Value = ReadTask(result);
                    sheet.Cell(row, 7).Value = result.ResultType ?? string.Empty;
                    sheet.Cell(row, 8).Value = ReadDetectionCount(result);
                    sheet.Cell(row, 9).Value = result.Confidence ?? 0d;

                    if (result.Confidence.HasValue)
                        sheet.Cell(row, 9).Style.NumberFormat.Format = "0.00%";

                    sheet.Cell(row, 10).Value = result.Status.ToString();
                    sheet.Cell(row, 11).Value = result.ProcessedAt;
                    sheet.Cell(row, 12).Value = result.ResultJson ?? string.Empty;

                    row++;
                }
            }

            sheet.Columns().AdjustToContents(1, 11);
            sheet.Column(12).Width = 60;
            sheet.SheetView.FreezeRows(1);

            if (row > 2)
                sheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        }

        private static void BuildMediaSheet(XLWorkbook workbook, PipelineRun run)
        {
            var sheet = workbook.Worksheets.Add("Media");

            var headers = new[] { "MediaFile", "SizeBytes", "DurationSeconds", "ContentType", "Status", "ProcessedAt", "Notes" };

            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = sheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");

            var row = 2;

            foreach (var runVideo in run.PipelineRunVideos ?? new List<PipelineRunVideo>())
            {
                sheet.Cell(row, 1).Value = runVideo.Video?.FileName ?? string.Empty;
                sheet.Cell(row, 2).Value = runVideo.Video?.SizeBytes ?? 0;
                sheet.Cell(row, 3).Value = runVideo.Video?.Duration?.TotalSeconds ?? 0;
                sheet.Cell(row, 4).Value = runVideo.Video?.ContentType ?? string.Empty;
                sheet.Cell(row, 5).Value = runVideo.Status.ToString();
                sheet.Cell(row, 6).Value = runVideo.ProcessedAt;
                sheet.Cell(row, 7).Value = runVideo.Notes ?? string.Empty;
                row++;
            }

            sheet.Columns().AdjustToContents();
            sheet.SheetView.FreezeRows(1);
        }

        private static int ReadDetectionCount(PipelineResult result)
        {
            if (string.IsNullOrWhiteSpace(result.ResultJson))
                return 0;

            try
            {
                using var document = JsonDocument.Parse(result.ResultJson);
                var root = document.RootElement;

                foreach (var name in new[] { "totalDetections", "count", "TotalDetections", "Count" })
                {
                    if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
                        return value.GetInt32();
                }

                if (root.TryGetProperty("detections", out var detections) &&
                    detections.ValueKind == JsonValueKind.Array)
                {
                    return detections.GetArrayLength();
                }
            }
            catch (JsonException)
            {
            }

            return 0;
        }

        private static string ReadTask(PipelineResult result)
        {
            if (string.IsNullOrWhiteSpace(result.ResultJson))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(result.ResultJson);
                var root = document.RootElement;

                if (root.TryGetProperty("task", out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
            }

            return string.Empty;
        }
    }
}
