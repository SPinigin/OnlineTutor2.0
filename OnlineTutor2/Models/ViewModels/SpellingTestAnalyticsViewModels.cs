using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class SpellingTestAnalyticsViewModel
    {
        public SpellingTest SpellingTest { get; set; }
        public SpellingTestStatistics Statistics { get; set; }
        public List<StudentSpellingTestResultViewModel> StudentSpellingResults { get; set; } = new List<StudentSpellingTestResultViewModel>();
        public List<SpellingQuestionAnalyticsViewModel> SpellingQuestionAnalytics { get; set; } = new List<SpellingQuestionAnalyticsViewModel>();
    }

    public class SpellingTestStatistics
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

    public class StudentSpellingTestResultViewModel
    {
        public Student Student { get; set; }
        public List<SpellingTestResult> Results { get; set; } = new List<SpellingTestResult>();
        public SpellingTestResult? BestResult { get; set; }
        public SpellingTestResult? LatestResult { get; set; }
        public int AttemptsUsed { get; set; }
        public bool HasCompleted { get; set; }
        public bool IsInProgress { get; set; }
        public TimeSpan? TotalTimeSpent { get; set; }
    }

    public class SpellingQuestionAnalyticsViewModel
    {
        public SpellingQuestion Question { get; set; }
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double SuccessRate { get; set; }
        public List<CommonMistakeViewModel> CommonMistakes { get; set; } = new List<CommonMistakeViewModel>();
        public bool IsMostDifficult { get; set; }
        public bool IsEasiest { get; set; }
    }

    public class CommonMistakeViewModel
    {
        public string IncorrectAnswer { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public List<string> StudentNames { get; set; } = new List<string>();
    }
}
