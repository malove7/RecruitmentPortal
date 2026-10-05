using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<JobPosition> JobPositions { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<CandidateNote> CandidateNotes { get; set; }
        public DbSet<CandidateEvaluationForm> CandidateEvaluationForms { get; set; }
        public DbSet<WorkExperienceRecord> WorkExperienceRecords { get; set; }
        public DbSet<EducationRecord> EducationRecords { get; set; }
        public DbSet<CandidateFeedbackToken> CandidateFeedbackTokens { get; set; }
        public DbSet<TechnicalEvaluationRecord> TechnicalEvaluationRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraints or other specifics if needed
            modelBuilder.Entity<Candidate>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<CandidateEvaluationForm>()
                .HasIndex(f => f.SubmissionToken)
                .IsUnique()
                .HasFilter("[SubmissionToken] IS NOT NULL");

            modelBuilder.Entity<CandidateFeedbackToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<TechnicalEvaluationRecord>()
                .HasOne(t => t.CandidateEvaluationForm)
                .WithMany(f => f.TechnicalEvaluations)
                .HasForeignKey(t => t.CandidateEvaluationFormId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
