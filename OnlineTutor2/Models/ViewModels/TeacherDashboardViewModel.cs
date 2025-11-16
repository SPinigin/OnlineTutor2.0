using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class TeacherDashboardViewModel
    {
        public ApplicationUser Teacher { get; set; }
        public List<SpellingTest> SpellingTests { get; set; } = new();
        public List<PunctuationTest> PunctuationTests { get; set; } = new();
        public List<OrthoeopyTest> OrthoeopyTests { get; set; } = new();
        public List<RegularTest> RegularTests { get; set; } = new();

        public int TotalActiveTests =>
            SpellingTests.Count +
            PunctuationTests.Count +
            OrthoeopyTests.Count +
            RegularTests.Count;

        public int TotalStudentsInProgress =>
            SpellingTests.Sum(t => t.SpellingTestResults.Count(r => !r.IsCompleted)) +
            PunctuationTests.Sum(t => t.PunctuationTestResults.Count(r => !r.IsCompleted)) +
            OrthoeopyTests.Sum(t => t.OrthoeopyTestResults.Count(r => !r.IsCompleted)) +
            RegularTests.Sum(t => t.RegularTestResults.Count(r => !r.IsCompleted));

        public int TotalCompletedToday
        {
            get
            {
                var today = DateTime.Today;
                return SpellingTests.Sum(t => t.SpellingTestResults.Count(r => r.IsCompleted && r.CompletedAt.HasValue && r.CompletedAt.Value.Date == today)) +
                       PunctuationTests.Sum(t => t.PunctuationTestResults.Count(r => r.IsCompleted && r.CompletedAt.HasValue && r.CompletedAt.Value.Date == today)) +
                       OrthoeopyTests.Sum(t => t.OrthoeopyTestResults.Count(r => r.IsCompleted && r.CompletedAt.HasValue && r.CompletedAt.Value.Date == today)) +
                       RegularTests.Sum(t => t.RegularTestResults.Count(r => r.IsCompleted && r.CompletedAt.HasValue && r.CompletedAt.Value.Date == today));
            }
        }
    }

    public class TestActivityViewModel
    {
        public int TestId { get; set; }
        public string TestTitle { get; set; }
        public string TestType { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Status { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
    }
}
