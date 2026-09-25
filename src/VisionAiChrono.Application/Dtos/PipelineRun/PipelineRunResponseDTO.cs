namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class PipelineRunResponseDTO
    {
        public Guid Id { get; set; }
        public Guid PipelineId { get; set; }
        public string PipelineName { get; set; } = string.Empty;
        public Guid? StartedById { get; set; }
        public string? StartedByName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ICollection<PipelineRunVideoResponseDTO> PipelineRunVideos { get; set; } = new List<PipelineRunVideoResponseDTO>();
        public ICollection<PipelineRunModelResponseDTO> PipelineRunModels { get; set; } = new List<PipelineRunModelResponseDTO>();
    }
}
