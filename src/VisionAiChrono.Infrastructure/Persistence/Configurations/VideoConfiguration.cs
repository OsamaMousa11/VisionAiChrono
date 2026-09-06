using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class VideoConfiguration : IEntityTypeConfiguration<Video>
    {
        public void Configure(EntityTypeBuilder<Video> builder)
        {
            builder.ToTable("Videos");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.FileName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(v => v.FilePath)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(v => v.ContentType)
                .HasMaxLength(200);

            builder.HasMany(v => v.PipelineRunVideos)
                .WithOne(prv => prv.Video)
                .HasForeignKey(prv => prv.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}