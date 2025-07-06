using System.ComponentModel.DataAnnotations;
using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class CreatePunctuationTestViewModel
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

    public class PunctuationQuestionImportViewModel
    {
        public int PunctuationTestId { get; set; }

        [Required(ErrorMessage = "Выберите файл для импорта")]
        [Display(Name = "Excel файл с вопросами")]
        public IFormFile ExcelFile { get; set; }

        [Display(Name = "Баллы за каждый правильный ответ")]
        [Range(1, 10, ErrorMessage = "Баллы должны быть от 1 до 10")]
        public int PointsPerQuestion { get; set; } = 1;
    }

    public class ImportPunctuationQuestionRow
    {
        public int RowNumber { get; set; }
        public string? SentenceWithNumbers { get; set; }
        public string? CorrectPositions { get; set; }
        public string? PlainSentence { get; set; }
        public string? Hint { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsValid => !Errors.Any();
    }

    public class TakePunctuationTestViewModel
    {
        public PunctuationTestResult TestResult { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public int CurrentQuestionIndex { get; set; }
    }

    public class SubmitPunctuationAnswerViewModel
    {
        [Required]
        public int TestResultId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [StringLength(50)]
        public string? StudentAnswer { get; set; }
    }

    public class StudentPunctuationTestIndexViewModel
    {
        public Student Student { get; set; }
        public List<PunctuationTest> AvailableTests { get; set; } = new List<PunctuationTest>();
    }
}
