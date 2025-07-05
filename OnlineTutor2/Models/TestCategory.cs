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
    }
}
