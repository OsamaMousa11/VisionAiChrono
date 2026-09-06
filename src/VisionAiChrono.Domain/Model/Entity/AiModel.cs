using System;
using System.Collections.Generic;
using CleanArchitectureTemplate_Domain.Common;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class AiModel : BaseEntity
    {
        public AiModel()
        {
            PipelineModels = new List<PipelineModel>();
            PipelineRunModels = new List<PipelineRunModel>();
            PipelineResults = new List<PipelineResult>();
            CreatedAt = DateTime.UtcNow;
        }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ModelType { get; set; }
        public string? ConfigurationJson { get; set; }

        public ICollection<PipelineModel> PipelineModels { get; set; }
        public ICollection<PipelineRunModel> PipelineRunModels { get; set; }
        public ICollection<PipelineResult> PipelineResults { get; set; }
    }
}