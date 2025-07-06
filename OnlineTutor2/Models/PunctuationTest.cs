using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class PunctuationTest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
        public string? Description { get; set; }

        // Внешние ключи
        public string TeacherId { get; set; } = string.Empty;
        public int TestCategoryId { get; set; } = 5;
        public int? ClassId { get; set; }

        // Настройки теста
        [Range(5, 300, ErrorMessage = "Время должно быть от 5 до 300 минут")]
        public int TimeLimit { get; set; } = 30;

        [Range(1, 10, ErrorMessage = "Количество попыток должно быть от 1 до 10")]
        public int MaxAttempts { get; set; } = 3;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool ShowHints { get; set; } = true;
        public bool ShowCorrectAnswers { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; } = null!;
        public virtual TestCategory TestCategory { get; set; } = null!;
        public virtual Class? Class { get; set; }
        public virtual ICollection<PunctuationQuestion> Questions { get; set; } = new List<PunctuationQuestion>();
        public virtual ICollection<PunctuationTestResult> TestResults { get; set; } = new List<PunctuationTestResult>();
    }
}
