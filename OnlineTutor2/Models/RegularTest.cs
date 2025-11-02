using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularTest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string TeacherId { get; set; }
        public int TestCategoryId { get; set; } = 3;

        public int? ClassId { get; set; }

        public int TimeLimit { get; set; }

        public int MaxAttempts { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public TestType Type { get; set; }

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; }
        public virtual TestCategory TestCategory { get; set; } = null!;
        public virtual Class? Class { get; set; }
        public virtual ICollection<RegularQuestion> RegularQuestions { get; set; } = new List<RegularQuestion>();
        public virtual ICollection<RegularTestResult> RegularTestResults { get; set; } = new List<RegularTestResult>();
    }

    public enum TestType
    {
        Practice = 1,
        Quiz = 2,
        Exam = 3,
        Homework = 4
    }
}
