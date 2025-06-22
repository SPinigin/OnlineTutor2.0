using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class RegularTestAnalyticsViewModel
    {
        public Test Test { get; set; }
        public TestStatistics Statistics { get; set; }
        public List<RegularTestStudentResultViewModel> StudentResults { get; set; } = new List<RegularTestStudentResultViewModel>();
        public List<Student> StudentsNotTaken { get; set; } = new List<Student>();
        public List<RegularTestQuestionAnalyticsViewModel> QuestionAnalytics { get; set; } = new List<RegularTestQuestionAnalyticsViewModel>();
    }

    public class RegularTestStudentResultViewModel
    {
        public Student Student { get; set; }
        public List<TestResult> Results { get; set; } = new List<TestResult>();
        public TestResult? BestResult { get; set; }
        public TestResult? LatestResult { get; set; }
        public int AttemptsUsed { get; set; }
        public bool HasCompleted { get; set; }
        public bool IsInProgress { get; set; }
        public TimeSpan? TotalTimeSpent { get; set; }
    }

    public class RegularTestQuestionAnalyticsViewModel
    {
        public Question Question { get; set; }
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double SuccessRate { get; set; }
        public List<CommonMistakeViewModel> CommonMistakes { get; set; } = new List<CommonMistakeViewModel>();
        public bool IsMostDifficult { get; set; }
        public bool IsEasiest { get; set; }
    }
}
