using System;
using System.Collections.Generic;
using CleanArchitectureTemplate_Domain.Common;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class Pipeline : BaseEntity
    {
        public Pipeline()
        {
            PipelineModels = new List<PipelineModel>();
            PipelineRuns = new List<PipelineRun>();
            CreatedAt = DateTime.UtcNow;
        }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<PipelineModel> PipelineModels { get; set; }
        public ICollection<PipelineRun> PipelineRuns { get; set; }
    }
}