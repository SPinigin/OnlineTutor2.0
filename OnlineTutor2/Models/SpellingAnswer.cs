using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class SpellingAnswer
    {
        public int Id { get; set; }

        public int SpellingTestResultId { get; set; }

        public int SpellingQuestionId { get; set; }

        [StringLength(10)]
        public string? StudentAnswer { get; set; }

        public bool IsCorrect { get; set; }

        public int Points { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual SpellingTestResult TestResult { get; set; }
        public virtual SpellingQuestion Question { get; set; }
    }
}
