using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class PipelineRunConfiguration : IEntityTypeConfiguration<PipelineRun>
    {
        public void Configure(EntityTypeBuilder<PipelineRun> builder)
        {
            builder.ToTable("PipelineRuns");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.StartedAt)
                .IsRequired();

            builder.Property(r => r.CompletedAt)
                .IsRequired(false);

            builder.Property(r => r.Status)
                .IsRequired();

            builder.HasMany(r => r.PipelineRunVideos)
                .WithOne(v => v.PipelineRun)
                .HasForeignKey(v => v.PipelineRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // keep StartedById as simple FK-less property to avoid coupling identity changes here
            builder.Property(r => r.StartedById).IsRequired(false);
        }
    }
}