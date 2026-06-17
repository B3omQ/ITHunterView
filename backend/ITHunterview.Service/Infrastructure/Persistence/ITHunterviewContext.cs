using ITHunterview.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class ITHunterviewContext : DbContext
    {
        public ITHunterviewContext(DbContextOptions<ITHunterviewContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(256);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.JwtId).IsRequired();
                
                entity.HasOne(d => d.User)
                      .WithMany(p => p.RefreshTokens)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
