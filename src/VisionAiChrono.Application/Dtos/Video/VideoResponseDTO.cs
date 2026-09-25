namespace VisionAiChrono.Application.Dtos.Video
{
    public class VideoResponseDTO
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
