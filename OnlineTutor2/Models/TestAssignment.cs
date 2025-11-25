using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    /// <summary>
    /// Модель задания для группировки тестов
    /// Например: "Задание 9" содержит тесты по правописанию
    /// </summary>
    public class TestAssignment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название задания обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        [Display(Name = "Название задания")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Номер задания")]
        public int AssignmentNumber { get; set; }

        [Required]
        [Display(Name = "Тип тестов")]
        public int TestCategoryId { get; set; }

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [Display(Name = "Активно")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; } = null!;
        public virtual TestCategory TestCategory { get; set; } = null!;
        public virtual ICollection<SpellingTest> SpellingTests { get; set; } = new List<SpellingTest>();
        public virtual ICollection<PunctuationTest> PunctuationTests { get; set; } = new List<PunctuationTest>();
        public virtual ICollection<OrthoeopyTest> OrthoeopyTests { get; set; } = new List<OrthoeopyTest>();
        public virtual ICollection<RegularTest> RegularTests { get; set; } = new List<RegularTest>();
    }
}




