using System;
using System.Collections.Generic;
using CleanArchitectureTemplate_Domain.Common;
using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class PipelineRunVideo : BaseEntity
    {
        public PipelineRunVideo()
        {
            PipelineResults = new List<PipelineResult>();
            ProcessedAt = DateTime.UtcNow;
            Status = ExecutionStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid PipelineRunId { get; set; }
        public PipelineRun? PipelineRun { get; set; }

        public Guid VideoId { get; set; }
        public Video? Video { get; set; }

        public ExecutionStatus Status { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string? Notes { get; set; }

        public ICollection<PipelineResult> PipelineResults { get; set; }
    }
}