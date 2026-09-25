namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class RunExecutionResponseDTO
    {
        public Guid RunId { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int UploadedCount { get; set; }
        public int ModelCount { get; set; }
        public ICollection<UploadedMediaResultDTO> UploadedMedia { get; set; } = new List<UploadedMediaResultDTO>();
    }
}
