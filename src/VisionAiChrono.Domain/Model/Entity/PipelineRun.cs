using System;
using System.Collections.Generic;
using CleanArchitectureTemplate_Domain.Common;
using CleanArchitectureTemplate_Domain.Model.Identity;
using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class PipelineRun : BaseEntity
    {
        public PipelineRun()
        {
            PipelineRunVideos = new List<PipelineRunVideo>();
            PipelineRunModels = new List<PipelineRunModel>();
            StartedAt = DateTime.UtcNow;
            Status = ExecutionStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid PipelineId { get; set; }
        public Pipeline? Pipeline { get; set; }

        public Guid? StartedById { get; set; }
        public ApplicationUser? StartedBy { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ExecutionStatus Status { get; set; }

        public ICollection<PipelineRunVideo> PipelineRunVideos { get; set; }
        public ICollection<PipelineRunModel> PipelineRunModels { get; set; }
    }
}