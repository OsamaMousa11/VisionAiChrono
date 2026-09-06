using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class PipelineModelConfiguration : IEntityTypeConfiguration<PipelineModel>
    {
        public void Configure(EntityTypeBuilder<PipelineModel> builder)
        {
            builder.ToTable("PipelineModels");

            builder.HasKey(pm => pm.Id);

            builder.Property(pm => pm.Order)
                .IsRequired();

            builder.Property(pm => pm.ConfigurationJson)
                .HasColumnType("nvarchar(max)");

            // prevent duplicate association entries for same pipeline + model
            builder.HasIndex(pm => new { pm.PipelineId, pm.AiModelId })
                .IsUnique();
        }
    }
}