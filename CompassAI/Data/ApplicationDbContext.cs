using System.Text.Json;
using CompassAI.Models;
using CompassAI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Data
{
    public class ApplicationDbContext : DbContext
    {
        private static readonly Guid InitialAdminId = Guid.Parse("a0c8d8d2-9e4f-4f7c-89e6-06ea39d0d3df");
        private static readonly Guid InitialAdminApiKeyId = Guid.Parse("6e26344e-971b-420c-8fe7-0fbc9d0fe520");
        private static readonly DateTime InitialAdminCreatedAt = new(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

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

            // The password is the BCrypt hash of the requested initial password.
            // Keep these values constant: EF Core writes this user through a migration.
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = InitialAdminId,
                Name = "Rafat Kamel",
                Email = "rafatkamel96@gmail.com",
                PasswordHash = "$2b$12$/EwnxUm7UQu84.BxsTLhDuuaPbRzYD.ZRwy3J88bNZ0hMTObkLUN6",
                Role = "admin",
                Active = true,
                EmailActive = true,
                Photo = "none",   
                LoginLogs = new List<DateTime>(),
                LogoutLogs = new List<DateTime>(),
                CurrentPlan = "Free",
                CreatedAt = InitialAdminCreatedAt,
                UpdatedAt = InitialAdminCreatedAt
            });

            // This key is seeded with the admin user through EF Core migrations.
            modelBuilder.Entity<ApiKey>().HasData(new ApiKey
            {
                Id = InitialAdminApiKeyId,
                Key = "cmp_eb4fbf10989d40e5a9b3c16d7e2f503a",
                UserId = InitialAdminId,
                PackageType = "Premium",
                RequestsLimit = 50000,
                RequestsUsed = 0,
                MapTalkLimit = 50000,
                SpecReviewerLimit = 50000,
                DocQueryLimit = 50000,
                ArcProMCP = 50000,
                QGISMCP = 50000,
                IsActive = true,
                CreatedAt = InitialAdminCreatedAt,
                ExpiresAt = null
            });
        }
    }
}
