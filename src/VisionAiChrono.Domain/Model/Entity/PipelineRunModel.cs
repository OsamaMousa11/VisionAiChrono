using System;
using CleanArchitectureTemplate_Domain.Common;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class PipelineRunModel : BaseEntity
    {
        public PipelineRunModel()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Guid PipelineRunId { get; set; }
        public PipelineRun? PipelineRun { get; set; }

        public Guid? AiModelId { get; set; }
        public AiModel? AiModel { get; set; }

        /// <summary>
        /// 0 = person, 1 = weapon, 2 = fire. Sent to the vision detection service.
        /// </summary>
        public int TaskIndex { get; set; }

        public int Order { get; set; }

        public string? ConfigurationJson { get; set; }
    }
}
