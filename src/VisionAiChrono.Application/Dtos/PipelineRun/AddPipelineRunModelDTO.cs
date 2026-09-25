namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class AddPipelineRunModelDTO
    {
        public Guid AiModelId { get; set; }
        public int Order { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
