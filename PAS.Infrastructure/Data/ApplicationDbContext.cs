using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;

namespace PAS.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProjectProposal> ProjectProposals { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<SupervisorProfile> SupervisorProfiles { get; set; }
        public DbSet<SupervisorTopic> SupervisorTopics { get; set; }
        public DbSet<StudentPreference> StudentPreferences { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SupervisorApprovalRequest> SupervisorApprovalRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectProposal>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectProposal>()
                .HasOne(p => p.MatchedSupervisor)
                .WithMany()
                .HasForeignKey(p => p.MatchedSupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectProposal>()
                .HasOne(p => p.ResearchArea)
                .WithMany()
                .HasForeignKey(p => p.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupervisorProfile>()
                .HasOne(s => s.Supervisor)
                .WithMany()
                .HasForeignKey(s => s.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupervisorProfile>()
                .HasOne(s => s.ResearchArea)
                .WithMany()
                .HasForeignKey(s => s.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupervisorTopic>()
                .HasOne(t => t.Supervisor)
                .WithMany()
                .HasForeignKey(t => t.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupervisorTopic>()
                .HasOne(t => t.ResearchArea)
                .WithMany()
                .HasForeignKey(t => t.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentPreference>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentPreference>()
                .HasOne(p => p.ProjectProposal)
                .WithMany()
                .HasForeignKey(p => p.ProjectProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupervisorApprovalRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}