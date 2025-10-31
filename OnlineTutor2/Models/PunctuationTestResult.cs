namespace OnlineTutor2.Models
{
    public class PunctuationTestResult
    {
        public int Id { get; set; }

        public int PunctuationTestId { get; set; }

        public int StudentId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int Score { get; set; }

        public int MaxScore { get; set; }

        public double Percentage { get; set; }

        public bool IsCompleted { get; set; }

        public int AttemptNumber { get; set; }

        // Навигационные свойства
        public virtual PunctuationTest PunctuationTest { get; set; }
        public virtual Student Student { get; set; }
        public virtual ICollection<PunctuationAnswer> Answers { get; set; } = new List<PunctuationAnswer>();
    }
}
