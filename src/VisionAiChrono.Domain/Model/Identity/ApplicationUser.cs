
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionAiChrono.Domain.Model.Entity;
using VisionAiChrono.Domain.Model.Identity;

namespace CleanArchitectureTemplate_Domain.Model.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public ApplicationUser()
        {
            RefreshTokens = new List<RefreshToken>();
            PipelineRuns = new List<PipelineRun>();
        }

        public string FullName { get; set; } = null!;

      


        public ICollection<RefreshToken>? RefreshTokens { get; set; }
        public ICollection<PipelineRun>? PipelineRuns { get; set; }
        public string? EmailConfirmationOtp { get; set; }
        public DateTime? OtpExpiration { get; set; }
 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsSuspended { get; set; }
        public string? SuspendReason { get; set; }
     
        public string? OtpCode { get; set; }
        public DateTime? LastOtpSentAt { get; set; }
    }
}
