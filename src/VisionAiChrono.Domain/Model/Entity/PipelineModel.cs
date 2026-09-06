using System;
using CleanArchitectureTemplate_Domain.Common;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class PipelineModel : BaseEntity
    {
        public PipelineModel()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Guid PipelineId { get; set; }
        public Pipeline? Pipeline { get; set; }

        public Guid AiModelId { get; set; }
        public AiModel? AiModel { get; set; }

        public int Order { get; set; }

        public string? ConfigurationJson { get; set; }
    }
}