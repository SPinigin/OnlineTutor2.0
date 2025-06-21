using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Test
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string TeacherId { get; set; }

        public int? ClassId { get; set; }

        public int TimeLimit { get; set; } // в минутах

        public int MaxAttempts { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public TestType Type { get; set; }

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; }
        public virtual Class? Class { get; set; }
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }

    public enum TestType
    {
        Practice = 1,
        Quiz = 2,
        Exam = 3,
        Homework = 4
    }
}
