using System;
using CleanArchitectureTemplate_Domain.Common;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class PipelineRunExport : BaseEntity
    {
        public PipelineRunExport()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Guid PipelineRunId { get; set; }
        public PipelineRun? PipelineRun { get; set; }

        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public long SizeBytes { get; set; }
        public int RowCount { get; set; }
    }
}
