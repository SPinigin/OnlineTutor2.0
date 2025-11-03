using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularAnswer
    {
        public int Id { get; set; }

        public int TestResultId { get; set; }
        public int QuestionId { get; set; }

        // Для Single/Multiple Choice - ID выбранных вариантов
        [StringLength(200)]
        public string? SelectedOptionIds { get; set; }

        // Для текстовых ответов (если будут)
        [StringLength(2000)]
        public string? StudentAnswer { get; set; }

        public bool IsCorrect { get; set; }
        public int Points { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual RegularTestResult RegularTestResult { get; set; } = null!;
        public virtual RegularQuestion RegularQuestion { get; set; } = null!;
    }
}
