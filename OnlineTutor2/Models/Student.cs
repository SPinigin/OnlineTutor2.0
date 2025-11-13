using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? ClassId { get; set; }

        [MaxLength(50)]
        public string? StudentNumber { get; set; }

        [StringLength(200)]
        public string? School { get; set; }

        public int? Grade { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ApplicationUser User { get; set; }
        public virtual Class? Class { get; set; }
        public virtual ICollection<RegularTestResult> RegularTestResults { get; set; } = new List<RegularTestResult>();
        public virtual ICollection<SpellingTestResult> SpellingTestResults { get; set; } = new List<SpellingTestResult>();
        public virtual ICollection<PunctuationTestResult> PunctuationTestResults { get; set; } = new List<PunctuationTestResult>();
        public virtual ICollection<OrthoeopyTestResult> OrthoeopyTestResults { get; set; } = new List<OrthoeopyTestResult>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
