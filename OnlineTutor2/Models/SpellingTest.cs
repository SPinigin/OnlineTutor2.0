using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class SpellingTest
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

        public int TimeLimit { get; set; } = 30; // в минутах

        public int MaxAttempts { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool ShowHints { get; set; } = true; // Показывать ли подсказки

        public bool ShowCorrectAnswers { get; set; } = true; // Показывать ли правильные ответы после теста

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; }
        public virtual Class? Class { get; set; }
        public virtual ICollection<SpellingQuestion> Questions { get; set; } = new List<SpellingQuestion>();
        public virtual ICollection<SpellingTestResult> TestResults { get; set; } = new List<SpellingTestResult>();
        public virtual TestCategory TestCategory { get; set; }
    }
}
