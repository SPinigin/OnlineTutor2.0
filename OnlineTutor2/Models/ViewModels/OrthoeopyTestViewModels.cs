using System.ComponentModel.DataAnnotations;
using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class CreateOrthoeopyTestViewModel
    {
        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        [Display(Name = "Название теста")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Онлайн-класс")]
        public int? ClassId { get; set; }

        [Required]
        [Range(5, 300)]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; } = 30;

        [Required]
        [Range(1, 10)]
        [Display(Name = "Максимальное количество попыток")]
        public int MaxAttempts { get; set; } = 3;

        [Display(Name = "Дата начала")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Показывать подсказки")]
        public bool ShowHints { get; set; } = true;

        [Display(Name = "Показывать правильные ответы")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "Тест активен")]
        public bool IsActive { get; set; } = true;
    }

    public class OrthoeopyQuestionImportViewModel
    {
        [Required]
        public int OrthoeopyTestId { get; set; }

        [Required(ErrorMessage = "Выберите файл Excel для импорта")]
        [Display(Name = "Файл Excel с вопросами")]
        public IFormFile ExcelFile { get; set; } = null!;

        [Required]
        [Range(1, 10, ErrorMessage = "Баллы должны быть от 1 до 10")]
        [Display(Name = "Баллы за каждый правильный ответ")]
        public int PointsPerQuestion { get; set; } = 1;
    }

    public class ImportOrthoeopyQuestionRow
    {
        public int RowNumber { get; set; }
        public string Word { get; set; } = string.Empty;
        public int StressPosition { get; set; }
        public string WordWithStress { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string? WrongStressPositions { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class TakeOrthoeopyTestViewModel
    {
        public OrthoeopyTestResult TestResult { get; set; } = null!;
        public TimeSpan TimeRemaining { get; set; }
        public int CurrentQuestionIndex { get; set; }
    }

    public class SubmitOrthoeopyAnswerViewModel
    {
        [Required]
        public int TestResultId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [StringLength(50)]
        public string? StudentAnswer { get; set; }
    }

    public class StudentOrthoeopyTestIndexViewModel
    {
        public Student Student { get; set; }
        public List<OrthoeopyTest> AvailableTests { get; set; } = new List<OrthoeopyTest>();
    }
}
