namespace OnlineTutor2.Models
{
    public class TestResult
    {
        public int Id { get; set; }

        public int TestId { get; set; }

        public int StudentId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int Score { get; set; }

        public int MaxScore { get; set; }

        public double Percentage { get; set; }

        public bool IsCompleted { get; set; }

        public int AttemptNumber { get; set; }

        // Навигационные свойства
        public virtual Test Test { get; set; }
        public virtual Student Student { get; set; }
        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
}
