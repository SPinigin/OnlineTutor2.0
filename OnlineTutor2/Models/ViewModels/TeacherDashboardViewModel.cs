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
                int spellingInProgress = 0;
                foreach (var test in SpellingTests)
                {
                    foreach (var result in test.SpellingTestResults)
                    {
                        if (!result.IsCompleted) spellingInProgress++;
                    }
                }
                
                int punctuationInProgress = 0;
                foreach (var test in PunctuationTests)
                {
                    foreach (var result in test.PunctuationTestResults)
                    {
                        if (!result.IsCompleted) punctuationInProgress++;
                    }
                }
                
                int orthoeopyInProgress = 0;
                foreach (var test in OrthoeopyTests)
                {
                    foreach (var result in test.OrthoeopyTestResults)
                    {
                        if (!result.IsCompleted) orthoeopyInProgress++;
                    }
                }
                
                int regularInProgress = 0;
                foreach (var test in RegularTests)
                {
                    foreach (var result in test.RegularTestResults)
                    {
                        if (!result.IsCompleted) regularInProgress++;
                    }
                }

                return spellingInProgress + punctuationInProgress + orthoeopyInProgress + regularInProgress;
            }
        }

        public int TotalCompletedToday
        {
            get
            {
                var today = DateTime.Today;

                int spellingCompleted = 0;
                foreach (var test in SpellingTests)
                {
                    foreach (var result in test.SpellingTestResults)
                    {
                        if (result.IsCompleted && result.CompletedAt.HasValue && result.CompletedAt.Value.Date == today)
                        {
                            spellingCompleted++;
                        }
                    }
                }

                int punctuationCompleted = 0;
                foreach (var test in PunctuationTests)
                {
                    foreach (var result in test.PunctuationTestResults)
                    {
                        if (result.IsCompleted && result.CompletedAt.HasValue && result.CompletedAt.Value.Date == today)
                        {
                            punctuationCompleted++;
                        }
                    }
                }

                int orthoeopyCompleted = 0;
                foreach (var test in OrthoeopyTests)
                {
                    foreach (var result in test.OrthoeopyTestResults)
                    {
                        if (result.IsCompleted && result.CompletedAt.HasValue && result.CompletedAt.Value.Date == today)
                        {
                            orthoeopyCompleted++;
                        }
                    }
                }

                int regularCompleted = 0;
                foreach (var test in RegularTests)
                {
                    foreach (var result in test.RegularTestResults)
                    {
                        if (result.IsCompleted && result.CompletedAt.HasValue && result.CompletedAt.Value.Date == today)
                        {
                            regularCompleted++;
                        }
                    }
                }

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
