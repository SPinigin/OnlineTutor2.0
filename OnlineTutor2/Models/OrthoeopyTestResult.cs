namespace OnlineTutor2.Models
{
    public class OrthoeopyTestResult
    {
        public int Id { get; set; }

        public int OrthoeopyTestId { get; set; }
        public int StudentId { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int Score { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }

        public bool IsCompleted { get; set; }
        public int AttemptNumber { get; set; }

        // Навигационные свойства
        public virtual OrthoeopyTest OrthoeopyTest { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
        public virtual ICollection<OrthoeopyAnswer> OrthoeopyAnswers { get; set; } = new List<OrthoeopyAnswer>();
    }
}
