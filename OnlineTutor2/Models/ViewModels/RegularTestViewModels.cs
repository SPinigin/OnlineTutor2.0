using OnlineTutor2.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateRegularTestViewModel
    {
        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200)]
        [Display(Name = "Название теста")]
        public string Title { get; set; }

        [StringLength(1000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Назначить онлайн-классу")]
        public int? ClassId { get; set; }

        [Range(5, 180, ErrorMessage = "Время должно быть от 5 до 180 минут")]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; } = 30;

        [Range(1, 10, ErrorMessage = "Количество попыток должно быть от 1 до 10")]
        [Display(Name = "Количество попыток")]
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

    public class StudentRegularTestIndexViewModel
    {
        public Student Student { get; set; }
        public List<RegularTest> AvailableTests { get; set; } = new List<RegularTest>();
    }

    public class TakeRegularTestViewModel
    {
        public RegularTestResult RegularTestResult { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public int CurrentQuestionIndex { get; set; }
    }

    public class SubmitRegularAnswerViewModel
    {
        [Required]
        public int TestResultId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [StringLength(10)]
        public string? StudentAnswer { get; set; }
    }

    public class RegularTestResultViewModel
    {
        public RegularTestResult RegularTestResult { get; set; }
        public List<RegularQuestionResultViewModel> RegularQuestionResults { get; set; } = new List<RegularQuestionResultViewModel>();
    }

    public class RegularQuestionResultViewModel
    {
        public RegularQuestion RegularQuestion { get; set; }
        public RegularAnswer? Answer { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
    }

    public class RegularQuestionImportViewModel
    {
        public int SpellingTestId { get; set; }

        [Required(ErrorMessage = "Выберите файл для импорта")]
        [Display(Name = "Excel файл с вопросами")]
        public IFormFile ExcelFile { get; set; }

        [Display(Name = "Баллы за каждый правильный ответ")]
        [Range(1, 10, ErrorMessage = "Баллы должны быть от 1 до 10")]
        public int PointsPerQuestion { get; set; } = 1;
    }

    public class ImportRegularQuestionRow
    {
        public int RowNumber { get; set; }
        public string? WordWithGap { get; set; }
        public string? CorrectLetter { get; set; }
        public string? FullWord { get; set; }
        public string? Hint { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsValid => !Errors.Any();
    }
}
