namespace VisionAiChrono.Application.Dtos.AiModel
{
    public class UpdateAiModelDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ModelType { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
