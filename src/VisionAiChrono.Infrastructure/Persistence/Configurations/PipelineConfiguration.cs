using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class PipelineConfiguration : IEntityTypeConfiguration<Pipeline>
    {
        public void Configure(EntityTypeBuilder<Pipeline> builder)
        {
            builder.ToTable("Pipelines");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.HasMany(p => p.PipelineModels)
                .WithOne(pm => pm.Pipeline)
                .HasForeignKey(pm => pm.PipelineId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.PipelineRuns)
                .WithOne(r => r.Pipeline)
                .HasForeignKey(r => r.PipelineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}