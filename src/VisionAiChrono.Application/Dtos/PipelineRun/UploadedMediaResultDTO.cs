namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class UploadedMediaResultDTO
    {
        public Guid MediaId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MediaKind { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
    }
}
