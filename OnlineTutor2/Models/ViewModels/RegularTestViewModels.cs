using OnlineTutor2.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    // Создание/редактирование теста
    public class CreateRegularTestViewModel
    {
        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200)]
        [Display(Name = "Название теста")]
        public string Title { get; set; } = "";

        [StringLength(1000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Назначить классу(ам)")]
        public List<int> SelectedClassIds { get; set; } = new List<int>();

        [Range(5, 300)]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; } = 30;

        [Range(1, 100)]
        [Display(Name = "Количество попыток")]
        public int MaxAttempts { get; set; } = 3;

        [Display(Name = "Дата начала")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Показывать подсказки")]
        public bool ShowHints { get; set; } = true;

        [Display(Name = "Показывать правильные ответы после завершения")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "Тест активен")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Тип теста")]
        public TestType TestType { get; set; } = TestType.Practice;
    }

    // Создание/редактирование вопроса
    public class CreateRegularQuestionViewModel
    {
        public int? Id { get; set; }
        public int TestId { get; set; }

        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [StringLength(1000)]
        [Display(Name = "Текст вопроса")]
        public string Text { get; set; } = "";

        [Required]
        [Display(Name = "Тип вопроса")]
        public QuestionType Type { get; set; } = QuestionType.SingleChoice;

        [Range(1, 100)]
        [Display(Name = "Баллы")]
        public int Points { get; set; } = 1;

        [StringLength(500)]
        [Display(Name = "Подсказка")]
        public string? Hint { get; set; }

        [StringLength(1000)]
        [Display(Name = "Объяснение")]
        public string? Explanation { get; set; }

        [Display(Name = "Порядковый номер")]
        public int OrderIndex { get; set; }

        // Варианты ответов
        public List<RegularQuestionOptionViewModel> Options { get; set; } = new();
    }

    // Вариант ответа
    public class RegularQuestionOptionViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; } = "";

        public bool IsCorrect { get; set; }

        public int OrderIndex { get; set; }

        [StringLength(500)]
        public string? Explanation { get; set; }
    }

    // Прохождение теста студентом
    public class TakeRegularTestViewModel
    {
        public RegularTestResult TestResult { get; set; } = null!;
        public TimeSpan TimeRemaining { get; set; }
        public int CurrentQuestionIndex { get; set; }
    }

    // Отправка ответа
    public class SubmitRegularAnswerViewModel
    {
        [Required]
        public int TestResultId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        // Для Single Choice - один ID
        public int? SelectedOptionId { get; set; }

        // Для Multiple Choice - несколько ID
        public List<int>? SelectedOptionIds { get; set; }

        // Для True/False
        public bool? TrueFalseAnswer { get; set; }
    }
}
