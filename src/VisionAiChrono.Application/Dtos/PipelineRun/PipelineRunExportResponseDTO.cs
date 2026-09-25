namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class PipelineRunExportResponseDTO
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public int RowCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
