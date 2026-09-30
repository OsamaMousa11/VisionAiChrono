namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class PipelineRunModelResponseDTO
    {
        public Guid Id { get; set; }
        public Guid PipelineRunId { get; set; }
        public Guid? AiModelId { get; set; }
        public string AiModelName { get; set; } = string.Empty;

        /// <summary>
        /// 0 = person, 1 = weapon, 2 = fire.
        /// </summary>
        public int TaskIndex { get; set; }

        public string TaskName { get; set; } = string.Empty;
        public int Order { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
