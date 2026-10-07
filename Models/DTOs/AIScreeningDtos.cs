using System.Text.Json.Serialization;

namespace RecruitmentPortal.Models.DTOs
{
    public class AIScreeningResponseDto
    {
        [JsonPropertyName("overallScore")]
        public int OverallScore { get; set; }

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = string.Empty;

        [JsonPropertyName("recruiterSummary")]
        public string RecruiterSummary { get; set; } = string.Empty;

        [JsonPropertyName("skills")]
        public List<AISkillEvaluationDto> Skills { get; set; } = new();

        [JsonPropertyName("strengths")]
        public List<string> Strengths { get; set; } = new();

        [JsonPropertyName("evidenceExtracted")]
        public List<AIEvidenceDto> EvidenceExtracted { get; set; } = new();

        [JsonPropertyName("concerns")]
        public List<string> Concerns { get; set; } = new();

        [JsonPropertyName("gaps")]
        public List<string> Gaps { get; set; } = new();

        [JsonPropertyName("experienceAnalysis")]
        public AIExperienceAnalysisDto ExperienceAnalysis { get; set; } = new();

        [JsonPropertyName("followUpQuestions")]
        public List<AIFollowUpQuestionDto> FollowUpQuestions { get; set; } = new();
    }

    public class AISkillEvaluationDto
    {
        [JsonPropertyName("skill")]
        public string Skill { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("maxScore")]
        public int MaxScore { get; set; } = 10;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("evidence")]
        public List<string> Evidence { get; set; } = new();

        [JsonPropertyName("gaps")]
        public List<string> Gaps { get; set; } = new();
    }

    public class AIEvidenceDto
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("evidence")]
        public string Evidence { get; set; } = string.Empty;
    }

    public class AIExperienceAnalysisDto
    {
        [JsonPropertyName("requiredExperience")]
        public string RequiredExperience { get; set; } = string.Empty;

        [JsonPropertyName("candidateExperience")]
        public string CandidateExperience { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;
    }

    public class AIFollowUpQuestionDto
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("skill")]
        public string Skill { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;
    }
}
