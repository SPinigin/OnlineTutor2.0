namespace OnlineTutor2.Models
{
    public class Grade
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int? AssignmentId { get; set; }

        public int? TestId { get; set; }

        public int Points { get; set; }

        public int MaxPoints { get; set; }

        public double Percentage { get; set; }

        public string? Comments { get; set; }

        public DateTime GradedAt { get; set; } = DateTime.Now;

        public string TeacherId { get; set; }

        // Навигационные свойства
        public virtual Student Student { get; set; }
        public virtual Assignment? Assignment { get; set; }
        public virtual RegularTest? Test { get; set; }
        public virtual ApplicationUser Teacher { get; set; }
    }
}
