namespace VisionAiChrono.Application.Dtos.Pipeline
{
    public class AddPipelineModelDTO
    {
        public Guid AiModelId { get; set; }
        public int Order { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
