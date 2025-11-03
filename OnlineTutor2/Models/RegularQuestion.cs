using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularQuestion
    {
        public int Id { get; set; }

        [Required]
        public int TestId { get; set; }

        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [StringLength(1000, ErrorMessage = "Текст вопроса не может превышать 1000 символов")]
        [Display(Name = "Текст вопроса")]
        public string Text { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Тип вопроса")]
        public QuestionType Type { get; set; } = QuestionType.SingleChoice;

        [Range(1, 100, ErrorMessage = "Баллы должны быть от 1 до 100")]
        [Display(Name = "Баллы за правильный ответ")]
        public int Points { get; set; } = 1;

        [Display(Name = "Порядковый номер")]
        public int OrderIndex { get; set; }

        [StringLength(500, ErrorMessage = "Подсказка не может превышать 500 символов")]
        [Display(Name = "Подсказка (необязательно)")]
        public string? Hint { get; set; }

        [StringLength(1000, ErrorMessage = "Объяснение не может превышать 1000 символов")]
        [Display(Name = "Объяснение правильного ответа")]
        public string? Explanation { get; set; }

        // Навигационные свойства
        public virtual RegularTest RegularTest { get; set; }
        public virtual ICollection<RegularQuestionOption> Options { get; set; } = new List<RegularQuestionOption>();
        public virtual ICollection<RegularAnswer> RegularAnswers { get; set; } = new List<RegularAnswer>();
    }

    public enum QuestionType
    {
        SingleChoice = 1,      // Одиночный выбор (один правильный ответ)
        MultipleChoice = 2,    // Множественный выбор (несколько правильных ответов)
        TrueFalse = 3          // Верно/Неверно
    }

    public static class QuestionTypeExtensions
    {
        public static string GetDisplayName(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "Одиночный выбор",
                QuestionType.MultipleChoice => "Множественный выбор",
                QuestionType.TrueFalse => "Верно/Неверно",
                _ => "Неизвестный тип"
            };
        }

        public static string GetIcon(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "fa-dot-circle",
                QuestionType.MultipleChoice => "fa-check-square",
                QuestionType.TrueFalse => "fa-question-circle",
                _ => "fa-question"
            };
        }

        public static string GetDescription(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "Выберите один правильный вариант ответа",
                QuestionType.MultipleChoice => "Выберите все правильные варианты ответов",
                QuestionType.TrueFalse => "Определите, верно или неверно утверждение",
                _ => ""
            };
        }
    }
}
