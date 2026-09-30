using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Application.Dtos.PipelineResult
{
    public class PipelineResultResponseDTO
    {
        public Guid Id { get; set; }
        public Guid PipelineRunVideoId { get; set; }
        public Guid? AiModelId { get; set; }
        public string AiModelName { get; set; } = string.Empty;

        /// <summary>
        /// 0 = person, 1 = weapon, 2 = fire.
        /// </summary>
        public int? TaskIndex { get; set; }

        public string TaskName { get; set; } = string.Empty;
        public string ResultJson { get; set; } = string.Empty;
        public double? Confidence { get; set; }
        public string? ResultType { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
