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
        public DbSet<AIResumeScreening> AIResumeScreenings { get; set; }
        public DbSet<AIResumeSkillEvaluation> AIResumeSkillEvaluations { get; set; }
        public DbSet<AIResumeStrength> AIResumeStrengths { get; set; }
        public DbSet<AIResumeEvidence> AIResumeEvidences { get; set; }
        public DbSet<AIResumeConcern> AIResumeConcerns { get; set; }
        public DbSet<AIResumeGap> AIResumeGaps { get; set; }
        public DbSet<AIResumeFollowUpQuestion> AIResumeFollowUpQuestions { get; set; }
        public DbSet<KnowledgeBase> KnowledgeBases { get; set; }
        public DbSet<KnowledgeBaseDocument> KnowledgeBaseDocuments { get; set; }
        public DbSet<KnowledgeBaseChunk> KnowledgeBaseChunks { get; set; }
        public DbSet<KnowledgeBaseChatSession> KnowledgeBaseChatSessions { get; set; }
        public DbSet<KnowledgeBaseChatMessage> KnowledgeBaseChatMessages { get; set; }

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

            // AI Resume Screening relationships
            modelBuilder.Entity<AIResumeScreening>()
                .HasOne(s => s.JobPosition)
                .WithMany()
                .HasForeignKey(s => s.JobPositionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AIResumeSkillEvaluation>()
                .HasOne(e => e.AIResumeScreening)
                .WithMany(s => s.Skills)
                .HasForeignKey(e => e.AIResumeScreeningId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AIResumeStrength>()
                .HasOne(e => e.AIResumeScreening)
                .WithMany(s => s.Strengths)
                .HasForeignKey(e => e.AIResumeScreeningId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AIResumeEvidence>()
                .HasOne(e => e.AIResumeScreening)
                .WithMany(s => s.EvidenceExtracted)
                .HasForeignKey(e => e.AIResumeScreeningId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AIResumeConcern>()
                .HasOne(e => e.AIResumeScreening)
                .WithMany(s => s.Concerns)
                .HasForeignKey(e => e.AIResumeScreeningId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AIResumeGap>()
                .HasOne(e => e.AIResumeScreening)
                .WithMany(s => s.Gaps)
                .HasForeignKey(e => e.AIResumeScreeningId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AIResumeFollowUpQuestion>()
                .HasOne(e => e.AIResumeScreening)
                .WithMany(s => s.FollowUpQuestions)
                .HasForeignKey(e => e.AIResumeScreeningId)
                .OnDelete(DeleteBehavior.Cascade);

            // Knowledge Base relationships
            modelBuilder.Entity<KnowledgeBase>()
                .HasOne(k => k.CreatedByUser)
                .WithMany()
                .HasForeignKey(k => k.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<KnowledgeBaseDocument>()
                .HasOne(d => d.KnowledgeBase)
                .WithMany(k => k.Documents)
                .HasForeignKey(d => d.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KnowledgeBaseDocument>()
                .HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<KnowledgeBaseChunk>()
                .HasOne(c => c.KnowledgeBaseDocument)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.KnowledgeBaseDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KnowledgeBaseChunk>()
                .HasIndex(c => c.VectorId);

            modelBuilder.Entity<KnowledgeBaseChatSession>()
                .HasOne(s => s.KnowledgeBase)
                .WithMany(k => k.ChatSessions)
                .HasForeignKey(s => s.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KnowledgeBaseChatSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<KnowledgeBaseChatMessage>()
                .HasOne(m => m.ChatSession)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
