using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class TestCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string IconClass { get; set; }

        [Required]
        [StringLength(20)]
        public string ColorClass { get; set; }

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual ICollection<SpellingTest> SpellingTests { get; set; } = new List<SpellingTest>();
        public virtual ICollection<PunctuationTest> PunctuationTests { get; set; } = new List<PunctuationTest>();
        public virtual ICollection<OrthoeopyTest> OrthoeopyTests { get; set; } = new List<OrthoeopyTest>();
        public virtual ICollection<RegularTest> RegularTests { get; set; } = new List<RegularTest>();
    }

    /// <summary>
    /// Константы категорий тестов для обеспечения согласованности
    /// </summary>
    public static class TestCategoryConstants
    {
        public const int Spelling = 1;      // Орфография
        public const int Punctuation = 2;   // Пунктуация
        public const int Orthoeopy = 3;     // Орфоэпия
        public const int Regular = 4;       // Классические тесты
    }
}
