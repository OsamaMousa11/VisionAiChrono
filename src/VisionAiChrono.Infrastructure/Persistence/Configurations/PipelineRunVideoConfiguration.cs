using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class PipelineRunVideoConfiguration : IEntityTypeConfiguration<PipelineRunVideo>
    {
        public void Configure(EntityTypeBuilder<PipelineRunVideo> builder)
        {
            builder.ToTable("PipelineRunVideos");

            builder.HasKey(prv => prv.Id);

            builder.Property(prv => prv.ProcessedAt)
                .IsRequired();

            builder.Property(prv => prv.Status)
                .IsRequired();

            builder.Property(prv => prv.Notes)
                .HasMaxLength(2000);

            builder.HasMany(prv => prv.PipelineResults)
                .WithOne(r => r.PipelineRunVideo)
                .HasForeignKey(r => r.PipelineRunVideoId)
                .OnDelete(DeleteBehavior.Cascade);

            // composite unique to prevent duplicate video entries in same run
            builder.HasIndex(prv => new { prv.PipelineRunId, prv.VideoId })
                .IsUnique();
        }
    }
}