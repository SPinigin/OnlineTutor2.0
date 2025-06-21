using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? ClassId { get; set; }

        [StringLength(20)]
        public string? StudentNumber { get; set; }

        [StringLength(200)]
        public string? School { get; set; }

        public int? Grade { get; set; } // Класс в школе (1-11)

        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ApplicationUser User { get; set; }
        public virtual Class? Class { get; set; }
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
