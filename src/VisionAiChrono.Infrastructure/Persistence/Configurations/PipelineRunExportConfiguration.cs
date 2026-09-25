using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class PipelineRunExportConfiguration : IEntityTypeConfiguration<PipelineRunExport>
    {
        public void Configure(EntityTypeBuilder<PipelineRunExport> builder)
        {
            builder.ToTable("PipelineRunExports");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(e => e.Content)
                .IsRequired();

            builder.HasOne(e => e.PipelineRun)
                .WithMany(r => r.Exports)
                .HasForeignKey(e => e.PipelineRunId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.PipelineRunId);
        }
    }
}
