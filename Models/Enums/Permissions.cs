using System.ComponentModel;

namespace RecruitmentPortal.Models.Enums
{
    public enum Permissions
    {
        // General
        [Description("View Dashboard")]
        ViewDashboard = 1,

        // Candidates
        [Description("View Candidates")]
        ViewCandidates = 10,
        [Description("Create Candidates")]
        CreateCandidates = 11,
        [Description("Edit Candidates")]
        EditCandidates = 12,
        [Description("Delete Candidates")]
        DeleteCandidates = 13,

        // JobPositions
        [Description("View Job Positions")]
        ViewJobPositions = 20,
        [Description("Create Job Positions")]
        CreateJobPositions = 21,
        [Description("Edit Job Positions")]
        EditJobPositions = 22,
        [Description("Delete Job Positions")]
        DeleteJobPositions = 23,

        // Interviews
        [Description("View Interviews")]
        ViewInterviews = 30,
        [Description("Create Interviews")]
        CreateInterviews = 31,
        [Description("Edit Interviews")]
        EditInterviews = 32,
        [Description("Delete Interviews")]
        DeleteInterviews = 33,

        // Interviewers
        [Description("View Interviewers")]
        ViewInterviewers = 40,
        [Description("Create Interviewers")]
        CreateInterviewers = 41,
        [Description("Edit Interviewers")]
        EditInterviewers = 42,
        [Description("Delete Interviewers")]
        DeleteInterviewers = 43,

        // Feedbacks
        [Description("View Feedbacks")]
        ViewFeedbacks = 50,
        [Description("Create Feedbacks")]
        CreateFeedbacks = 51,
        [Description("Edit Feedbacks")]
        EditFeedbacks = 52,
        [Description("Delete Feedbacks")]
        DeleteFeedbacks = 53
    }
}
