using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Infrastructure.Persistence.Configurations
{
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.ToTable("Favorites");

            builder.HasKey(f => f.Id);

            // Id is assigned in the entity (Guid.NewGuid()), do not let the DB generate it
            builder.Property(f => f.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(f => f.UserId)
                .IsRequired();

            builder.Property(f => f.PipelineId)
                .IsRequired();

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            // Relationship to ApplicationUser (ApplicationUser has no Favorites navigation in current model)
            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to Pipeline (Pipeline has no Favorites navigation in current model)
            builder.HasOne(f => f.Pipeline)
                .WithMany()
                .HasForeignKey(f => f.PipelineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent a user from favoriting the same pipeline more than once
            builder.HasIndex(f => new { f.UserId, f.PipelineId })
                .IsUnique();
        }
    }
}