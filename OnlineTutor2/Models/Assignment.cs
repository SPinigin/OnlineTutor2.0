using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace OnlineTutor2.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        public string TeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime DueDate { get; set; }

        public int MaxPoints { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual Class Class { get; set; }
        public virtual ApplicationUser Teacher { get; set; }
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
