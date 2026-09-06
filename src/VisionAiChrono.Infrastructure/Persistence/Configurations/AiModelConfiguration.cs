using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class AiModelConfiguration : IEntityTypeConfiguration<AiModel>
    {
        public void Configure(EntityTypeBuilder<AiModel> builder)
        {
            builder.ToTable("AiModels");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Description)
                .HasMaxLength(1000);

            builder.Property(m => m.ModelType)
                .HasMaxLength(200);

            builder.HasMany(m => m.PipelineModels)
                .WithOne(pm => pm.AiModel)
                .HasForeignKey(pm => pm.AiModelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.PipelineResults)
                .WithOne(r => r.AiModel)
                .HasForeignKey(r => r.AiModelId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}