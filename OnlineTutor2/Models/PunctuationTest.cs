using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class PunctuationTest
    {
        public int Id { get; set; }

        [Required]
        public int TestCategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string TeacherId { get; set; }

        public int? ClassId { get; set; }

        public int TimeLimit { get; set; } = 30;

        public int MaxAttempts { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool ShowHints { get; set; } = true;

        public bool ShowCorrectAnswers { get; set; } = true;

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; }
        public virtual Class? Class { get; set; }
        public virtual ICollection<PunctuationQuestion> Questions { get; set; } = new List<PunctuationQuestion>();
        public virtual ICollection<PunctuationTestResult> TestResults { get; set; } = new List<PunctuationTestResult>();
        public virtual TestCategory TestCategory { get; set; }
    }
}