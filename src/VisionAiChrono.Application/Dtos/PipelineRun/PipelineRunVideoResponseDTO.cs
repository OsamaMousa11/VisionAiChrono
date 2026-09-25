using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class PipelineRunVideoResponseDTO
    {
        public Guid Id { get; set; }
        public Guid PipelineRunId { get; set; }
        public Guid VideoId { get; set; }
        public string VideoFileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<PipelineResult.PipelineResultResponseDTO> PipelineResults { get; set; } = new List<PipelineResult.PipelineResultResponseDTO>();
    }
}
