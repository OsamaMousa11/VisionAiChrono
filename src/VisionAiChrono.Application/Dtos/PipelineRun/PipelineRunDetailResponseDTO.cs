namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class PipelineRunDetailResponseDTO
    {
        public Guid Id { get; set; }
        public Guid PipelineId { get; set; }
        public string PipelineName { get; set; } = string.Empty;
        public Guid? StartedById { get; set; }
        public string? StartedByName { get; set; }
        public string? StartedByEmail { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalMedia { get; set; }
        public int TotalModels { get; set; }
        public int TotalResults { get; set; }
        public double? AverageConfidence { get; set; }
        public ICollection<PipelineRunVideoResponseDTO> PipelineRunVideos { get; set; } = new List<PipelineRunVideoResponseDTO>();
        public ICollection<PipelineRunModelResponseDTO> PipelineRunModels { get; set; } = new List<PipelineRunModelResponseDTO>();
        public ICollection<PipelineRunExportResponseDTO> Exports { get; set; } = new List<PipelineRunExportResponseDTO>();
    }
}
