using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [StringLength(100)]
        public string? Subject { get; set; }

        [StringLength(500)]
        public string? Education { get; set; }

        public int? Experience { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;

        // Навигационные свойства
        public virtual ApplicationUser User { get; set; }
    }
}
