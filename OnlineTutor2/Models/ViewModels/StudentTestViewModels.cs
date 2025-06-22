using OnlineTutor2.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class StudentTestIndexViewModel
    {
        public Student Student { get; set; }
        public List<SpellingTest> AvailableTests { get; set; } = new List<SpellingTest>();
    }

    public class TakeSpellingTestViewModel
    {
        public SpellingTestResult TestResult { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public int CurrentQuestionIndex { get; set; }
    }

    public class SubmitAnswerViewModel
    {
        [Required]
        public int TestResultId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [StringLength(10)]
        public string? StudentAnswer { get; set; }
    }

    public class TestResultViewModel
    {
        public SpellingTestResult TestResult { get; set; }
        public List<QuestionResultViewModel> QuestionResults { get; set; } = new List<QuestionResultViewModel>();
    }

    public class QuestionResultViewModel
    {
        public SpellingQuestion Question { get; set; }
        public SpellingAnswer? Answer { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
    }
}
