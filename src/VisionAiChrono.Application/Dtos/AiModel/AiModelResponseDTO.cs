namespace VisionAiChrono.Application.Dtos.AiModel
{
    public class AiModelResponseDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ModelType { get; set; }
        public string? ConfigurationJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
