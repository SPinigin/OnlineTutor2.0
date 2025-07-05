namespace OnlineTutor2.Models
{
    public class StudentAnswer
    {
        public int Id { get; set; }

        public int TestResultId { get; set; }

        public int QuestionId { get; set; }

        public int? AnswerId { get; set; }

        public string? TextAnswer { get; set; }

        public bool IsCorrect { get; set; }

        public int Points { get; set; }

        // Навигационные свойства
        public virtual TestResult TestResult { get; set; }
        public virtual Question Question { get; set; }
        public virtual Answer? Answer { get; set; }
    }
}
