using VisionAiChrono.Application.Dtos.PipelineResult;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class PipelineRunHistoryResponseDTO
    {
        public Guid Id { get; set; }
        public Guid PipelineId { get; set; }
        public string PipelineName { get; set; } = string.Empty;
        public string? StartedByName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int TotalMedia { get; set; }
        public int TotalModels { get; set; }
        public int TotalResults { get; set; }
        public double? AverageConfidence { get; set; }
        public bool HasExport { get; set; }
        public int DurationSeconds { get; set; }
    }
}
