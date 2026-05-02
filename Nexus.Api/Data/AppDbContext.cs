using Microsoft.EntityFrameworkCore;
using Nexus.Shared.Models;

namespace Nexus.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<TicketAssignee> TicketAssignees { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Prevent EF from cascading deleting everything on User delete
            modelBuilder.Entity<TicketAssignee>()
                .HasOne(ta => ta.User)
                .WithMany()
                .HasForeignKey(ta => ta.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketAssignee>()
                .HasOne(ta => ta.Ticket)
                .WithMany(t => t.Assignees)
                .HasForeignKey(ta => ta.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ActivityLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActivityLog>()
                .HasOne<Ticket>()
                .WithMany(t => t.ActivityLogs)
                .HasForeignKey(al => al.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}