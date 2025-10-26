using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class OrthoeopyAnswer
    {
        public int Id { get; set; }

        public int OrthoeopyTestResultId { get; set; }
        public int OrthoeopyQuestionId { get; set; }

        // Позиция ударения, выбранная учеником
        public int? SelectedStressPosition { get; set; }

        public bool IsCorrect { get; set; }
        public int Points { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual OrthoeopyTestResult TestResult { get; set; } = null!;
        public virtual OrthoeopyQuestion Question { get; set; } = null!;
    }
}
