using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PolySport.Models;

namespace PolySport.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<User> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchPlayer> MatchPlayers { get; set; }
        public DbSet<Goal> Goals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Zusammengesetzter Schlüssel für die MatchPlayer-Tabelle
            modelBuilder.Entity<MatchPlayer>()
                .HasKey(mp => new { mp.MatchId, mp.UserId });

            // 2. Verhindert Kettenlöschungen (Cascade Delete) bei Toren
            modelBuilder.Entity<Goal>()
                .HasOne(g => g.Scorer)
                .WithMany(u => u.GoalsScored)
                .HasForeignKey(g => g.ScorerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Goal>()
                .HasOne(g => g.Assist)
                .WithMany(u => u.Assists)
                .HasForeignKey(g => g.AssistId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
