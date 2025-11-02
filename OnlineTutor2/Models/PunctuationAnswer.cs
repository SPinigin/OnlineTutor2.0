using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class PunctuationAnswer
    {
        public int Id { get; set; }

        public int PunctuationTestResultId { get; set; }

        public int PunctuationQuestionId { get; set; }

        [StringLength(50)]
        public string? StudentAnswer { get; set; }

        public bool IsCorrect { get; set; }

        public int Points { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual PunctuationTestResult PunctuationTestResult { get; set; }
        public virtual PunctuationQuestion PunctuationQuestion { get; set; }
    }
}