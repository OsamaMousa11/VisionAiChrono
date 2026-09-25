using System.IO;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class UploadedMediaInput
    {
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long Length { get; set; }
        public Stream Content { get; set; } = Stream.Null;
    }
}
