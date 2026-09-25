namespace VisionAiChrono.Application.Dtos.AiModel
{
    public class CreateAiModelDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ModelType { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
