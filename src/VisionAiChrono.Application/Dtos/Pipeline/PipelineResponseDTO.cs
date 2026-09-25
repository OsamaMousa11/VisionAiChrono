namespace VisionAiChrono.Application.Dtos.Pipeline
{
    public class PipelineResponseDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<PipelineModelResponseDTO> PipelineModels { get; set; } = new List<PipelineModelResponseDTO>();
    }
}
