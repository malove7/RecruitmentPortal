namespace RecruitmentPortal.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCandidates { get; set; }
        public int TotalOpenPositions { get; set; }
        public int InterviewsScheduledToday { get; set; }
        public int PendingFeedbacks { get; set; }
        public List<Candidate> RecentCandidates { get; set; }
        public List<Interview> UpcomingInterviews { get; set; }
    }
}
