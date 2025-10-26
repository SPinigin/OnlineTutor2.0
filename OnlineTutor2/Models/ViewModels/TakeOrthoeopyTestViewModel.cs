using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class TakeOrthoeopyTestViewModel
    {
        public OrthoeopyTestResult TestResult { get; set; } = null!;
        public TimeSpan TimeRemaining { get; set; }
        public int CurrentQuestionIndex { get; set; }
    }
}
