using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual ICollection<Class> TeacherClasses { get; set; } = new List<Class>();
        public virtual ICollection<Test> CreatedTests { get; set; } = new List<Test>();
        public virtual Student? StudentProfile { get; set; }
        public virtual Teacher? TeacherProfile { get; set; }

        // Вычисляемое свойство для полного имени
        public string FullName => $"{FirstName} {LastName}";

        // Вычисляемый возраст
        public int Age => DateTime.Now.Year - DateOfBirth.Year - (DateTime.Now.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
    }
}
