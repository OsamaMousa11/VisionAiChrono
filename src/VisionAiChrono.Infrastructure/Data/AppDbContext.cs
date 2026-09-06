using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CleanArchitectureTemplate_Domain.Model.Identity;
using VisionAiChrono.Domain.Model.Entity;
using VisionAiChrono.Domain.Model.Identity;

namespace VisionAiChrono.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Existing
        public DbSet<EmailOtp> EmailOtps { get; set; }

        // Domain sets
        public DbSet<Pipeline> Pipelines { get; set; }
        public DbSet<AiModel> AiModels { get; set; }
        public DbSet<PipelineModel> PipelineModels { get; set; }
        public DbSet<PipelineRun> PipelineRuns { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<PipelineRunVideo> PipelineRunVideos { get; set; }
        public DbSet<PipelineResult> AiResults { get; set; }

        // Favorites
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Apply IEntityTypeConfiguration<T> classes from this assembly
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}