using OnlineTutor2.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateSpellingTestViewModel
    {
        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200)]
        [Display(Name = "Название теста")]
        public string Title { get; set; }

        [StringLength(1000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Назначить классу")]
        public int? ClassId { get; set; }

        [Range(5, 180, ErrorMessage = "Время должно быть от 5 до 180 минут")]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; } = 30;

        [Range(1, 10, ErrorMessage = "Количество попыток должно быть от 1 до 10")]
        [Display(Name = "Максимальное количество попыток")]
        public int MaxAttempts { get; set; } = 1;

        [Display(Name = "Дата начала")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Показывать подсказки")]
        public bool ShowHints { get; set; } = true;

        [Display(Name = "Показывать правильные ответы после теста")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "Активный")]
        public bool IsActive { get; set; } = true;
    }

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
