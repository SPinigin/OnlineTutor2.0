using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class PunctuationTestAnalyticsViewModel
    {
        public PunctuationTest PunctuationTest { get; set; } = null!;
        public PunctuationTestStatistics Statistics { get; set; } = new PunctuationTestStatistics();
        public List<PunctuationStudentResultViewModel> StudentResults { get; set; } = new List<PunctuationStudentResultViewModel>();
        public List<PunctuationQuestionAnalyticsViewModel> QuestionAnalytics { get; set; } = new List<PunctuationQuestionAnalyticsViewModel>();
        public List<Student> StudentsNotTaken { get; set; } = new List<Student>();
    }

    public class PunctuationTestStatistics
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

    public class PunctuationStudentResultViewModel
    {
        public Student Student { get; set; } = null!;
        public List<PunctuationTestResult> Results { get; set; } = new List<PunctuationTestResult>();
        public int AttemptsUsed { get; set; }
        public bool HasCompleted { get; set; }
        public bool IsInProgress { get; set; }
        public PunctuationTestResult? BestResult { get; set; }
        public PunctuationTestResult? LatestResult { get; set; }
        public TimeSpan? TotalTimeSpent { get; set; }
    }

    public class PunctuationQuestionAnalyticsViewModel
    {
        public PunctuationQuestion Question { get; set; } = null!;
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double SuccessRate { get; set; }
        public List<CommonMistakeViewModel> CommonMistakes { get; set; } = new List<CommonMistakeViewModel>();
        public bool IsMostDifficult { get; set; }
        public bool IsEasiest { get; set; }
    }
}
