using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class OrthoeopyTestAnalyticsViewModel
    {
        public OrthoeopyTest Test { get; set; } = null!;
        public TestStatistics Statistics { get; set; } = new();
        public List<OrthoeopyStudentResultViewModel> StudentResults { get; set; } = new();
        public List<OrthoeopyQuestionAnalyticsViewModel> QuestionAnalytics { get; set; } = new();
        public List<Student> StudentsNotTaken { get; set; } = new();
    }

    public class OrthoeopyStudentResultViewModel
    {
        public Student Student { get; set; } = null!;
        public List<OrthoeopyTestResult> Results { get; set; } = new();
        public int AttemptsUsed { get; set; }
        public bool HasCompleted { get; set; }
        public bool IsInProgress { get; set; }
        public OrthoeopyTestResult? BestResult { get; set; }
        public OrthoeopyTestResult? LatestResult { get; set; }
        public TimeSpan? TotalTimeSpent { get; set; }
    }

    public class OrthoeopyQuestionAnalyticsViewModel
    {
        public OrthoeopyQuestion Question { get; set; } = null!;
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double SuccessRate { get; set; }
        public bool IsMostDifficult { get; set; }
        public bool IsEasiest { get; set; }
        public List<StressPositionMistakeViewModel> CommonMistakes { get; set; } = new();
    }

    public class StressPositionMistakeViewModel
    {
        public int IncorrectPosition { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public List<string> StudentNames { get; set; } = new();
    }
}
