namespace OnlineTutor2.Models
{
    public class RegularAnswer
    {
        public int Id { get; set; }

        public int TestResultId { get; set; }

        public int QuestionId { get; set; }

        public int? AnswerId { get; set; }

        public int? StudentAnswer { get; set; }

        public bool IsCorrect { get; set; }

        public int Points { get; set; }

        // Навигационные свойства
        public virtual RegularTestResult RegularTestResult { get; set; }
        public virtual RegularQuestion RegularQuestion { get; set; }
    }
}
