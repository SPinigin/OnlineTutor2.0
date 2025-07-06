using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class StudentAllTestsIndexViewModel
    {
        public Student Student { get; set; } = null!;
        public IEnumerable<SpellingTest> SpellingTests { get; set; } = new List<SpellingTest>();
        public IEnumerable<PunctuationTest> PunctuationTests { get; set; } = new List<PunctuationTest>();
    }
}
