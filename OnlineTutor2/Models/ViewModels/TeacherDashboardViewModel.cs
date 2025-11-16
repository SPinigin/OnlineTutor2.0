using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class TeacherDashboardViewModel
    {
        public ApplicationUser Teacher { get; set; }
        public List<SpellingTest> SpellingTests { get; set; } = new List<SpellingTest>();
        public List<PunctuationTest> PunctuationTests { get; set; } = new List<PunctuationTest>();
        public List<OrthoeopyTest> OrthoeopyTests { get; set; } = new List<OrthoeopyTest>();
        public List<RegularTest> RegularTests { get; set; } = new List<RegularTest>();

        public int TotalActiveTests
        {
            get
            {
                return SpellingTests.Count +
                       PunctuationTests.Count +
                       OrthoeopyTests.Count +
                       RegularTests.Count;
            }
        }

        public int TotalStudentsInProgress
        {
            get
            {
                var spellingInProgress = SpellingTests.Sum(t => t.SpellingTestResults.Count(r => !r.IsCompleted));
                var punctuationInProgress = PunctuationTests.Sum(t => t.PunctuationTestResults.Count(r => !r.IsCompleted));
                var orthoeopyInProgress = OrthoeopyTests.Sum(t => t.OrthoeopyTestResults.Count(r => !r.IsCompleted));
                var regularInProgress = RegularTests.Sum(t => t.RegularTestResults.Count(r => !r.IsCompleted));

                return spellingInProgress + punctuationInProgress + orthoeopyInProgress + regularInProgress;
            }
        }

        public int TotalCompletedToday
        {
            get
            {
                var today = DateTime.Today;

                var spellingCompleted = SpellingTests.Sum(t =>
                    t.SpellingTestResults.Count(r =>
                        r.IsCompleted &&
                        r.CompletedAt.HasValue &&
                        r.CompletedAt.Value.Date == today));

                var punctuationCompleted = PunctuationTests.Sum(t =>
                    t.PunctuationTestResults.Count(r =>
                        r.IsCompleted &&
                        r.CompletedAt.HasValue &&
                        r.CompletedAt.Value.Date == today));

                var orthoeopyCompleted = OrthoeopyTests.Sum(t =>
                    t.OrthoeopyTestResults.Count(r =>
                        r.IsCompleted &&
                        r.CompletedAt.HasValue &&
                        r.CompletedAt.Value.Date == today));

                var regularCompleted = RegularTests.Sum(t =>
                    t.RegularTestResults.Count(r =>
                        r.IsCompleted &&
                        r.CompletedAt.HasValue &&
                        r.CompletedAt.Value.Date == today));

                return spellingCompleted + punctuationCompleted + orthoeopyCompleted + regularCompleted;
            }
        }
    }

    public class TestActivityViewModel
    {
        public int TestId { get; set; }
        public int TestResultId { get; set; }
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
