using System;
using CleanArchitectureTemplate_Domain.Common;
using CleanArchitectureTemplate_Domain.Model.Identity;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class Favorite : BaseEntity
    {
        public Favorite()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Guid UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public Guid PipelineId { get; set; }
        public virtual Pipeline Pipeline { get; set; }
    }
}