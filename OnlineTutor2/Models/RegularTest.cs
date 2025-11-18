using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularTest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        [Display(Name = "Название теста")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
        public string? Description { get; set; }

        // Внешние ключи
        public string TeacherId { get; set; } = string.Empty;
        public int TestCategoryId { get; set; }

        // Настройки теста
        [Range(5, 300, ErrorMessage = "Время должно быть от 5 до 300 минут")]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; }

        [Range(1, 100, ErrorMessage = "Количество попыток должно быть от 1 до 100")]
        [Display(Name = "Количество попыток")]
        public int MaxAttempts { get; set; } = 1;

        [Display(Name = "Дата начала")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Показывать подсказки")]
        public bool ShowHints { get; set; } = true;

        [Display(Name = "Показывать правильные ответы")]
        public bool ShowCorrectAnswers { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public TestType Type { get; set; }

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; }
        public virtual TestCategory TestCategory { get; set; } = null!;
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
