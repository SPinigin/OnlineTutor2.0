using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class StudentTestHistoryViewModel
    {
        public Student Student { get; set; } = null!;
        public IEnumerable<SpellingTestResult> SpellingResults { get; set; } = new List<SpellingTestResult>();
        public IEnumerable<PunctuationTestResult> PunctuationResults { get; set; } = new List<PunctuationTestResult>();
        public IEnumerable<OrthoeopyTestResult> OrthoeopyResults { get; set; } = new List<OrthoeopyTestResult>();
        public IEnumerable<RegularTestResult> RegularResults { get; set; } = new List<RegularTestResult>();
    }
}
