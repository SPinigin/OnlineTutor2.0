using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class RegularTestAnalyticsViewModel
    {
        public RegularTest Test { get; set; }
        public RegularTestStatistics Statistics { get; set; }
        public List<RegularTestStudentResultViewModel> StudentResults { get; set; } = new List<RegularTestStudentResultViewModel>();
        public List<RegularTestQuestionAnalyticsViewModel> QuestionAnalytics { get; set; } = new List<RegularTestQuestionAnalyticsViewModel>();
    }

    public class RegularTestStatistics
    {
        public int TotalStudents { get; set; }
        public int StudentsCompleted { get; set; }
        public int StudentsNotStarted { get; set; }
        public int StudentsInProgress { get; set; }
        public double AverageScore { get; set; }
        public double AveragePercentage { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public TimeSpan AverageCompletionTime { get; set; }
        public DateTime? FirstCompletion { get; set; }
        public DateTime? LastCompletion { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new Dictionary<string, int>();
    }

    public class RegularTestStudentResultViewModel
    {
        public Student Student { get; set; }
        public List<RegularTestResult> Results { get; set; } = new List<RegularTestResult>();
        public RegularTestResult? BestResult { get; set; }
        public RegularTestResult? LatestResult { get; set; }
        public int AttemptsUsed { get; set; }
        public bool HasCompleted { get; set; }
        public bool IsInProgress { get; set; }
        public TimeSpan? TotalTimeSpent { get; set; }
    }

    public class RegularTestQuestionAnalyticsViewModel
    {
        public RegularQuestion RegularQuestion { get; set; }
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double SuccessRate { get; set; }
        public List<CommonMistakeViewModel> CommonMistakes { get; set; } = new List<CommonMistakeViewModel>();
        public bool IsMostDifficult { get; set; }
        public bool IsEasiest { get; set; }
    }
}
