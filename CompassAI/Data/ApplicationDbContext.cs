using System.Text.Json;
using CompassAI.Models;
using CompassAI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<ModelFeedback> ModelFeedbacks { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ModelFeedback>()
                .Property(f => f.Type)
                .HasConversion<string>();
            // 1. User - Permissions Relationship
            modelBuilder.Entity<UserPermission>()
                .HasOne(p => p.User)
                .WithMany(u => u.Permissions)
                .HasForeignKey(p => p.UserId);

            // 2. User - ApiKeys Relationship (One-to-Many)
            modelBuilder.Entity<ApiKey>()
                .HasOne(a => a.User)
                .WithMany(u => u.ApiKeys)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If user is deleted, keys are deleted

            // 3. Json Conversions for Logs
            modelBuilder.Entity<User>()
             .Property(u => u.LoginLogs)
             .HasConversion(
                 v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                 v => JsonSerializer.Deserialize<List<DateTime>>(v, (JsonSerializerOptions)null!) ?? new List<DateTime>()
             );

            modelBuilder.Entity<User>()
                .Property(u => u.LogoutLogs)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<DateTime>>(v, (JsonSerializerOptions)null!) ?? new List<DateTime>()
                );

            // 4. API Key Configuration
            modelBuilder.Entity<ApiKey>()
                .HasIndex(a => a.Key)
                .IsUnique(); // Ensure no duplicate keys in DB
        }
    }
}