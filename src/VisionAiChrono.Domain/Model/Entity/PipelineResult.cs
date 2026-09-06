using System;
using CleanArchitectureTemplate_Domain.Common;
using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class PipelineResult : BaseEntity
    {
        public PipelineResult()
        {
            ProcessedAt = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid PipelineRunVideoId { get; set; }
        public PipelineRunVideo? PipelineRunVideo { get; set; }

        public Guid AiModelId { get; set; }
        public AiModel? AiModel { get; set; }

        public string ResultJson { get; set; } = null!;

        public double? Confidence { get; set; }
        public string? ResultType { get; set; }
        public DateTime ProcessedAt { get; set; }

        public ExecutionStatus Status { get; set; }
    }
}
