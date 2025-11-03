using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularQuestion
    {
        public int Id { get; set; }

        [Required]
        public int TestId { get; set; }

        [Required]
        public string Text { get; set; }

        public QuestionType Type { get; set; }

        public int Points { get; set; } = 1;

        public int OrderIndex { get; set; }

        // Навигационные свойства
        public virtual RegularTest RegularTest { get; set; }
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
