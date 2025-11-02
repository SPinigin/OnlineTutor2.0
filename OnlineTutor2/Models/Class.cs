using System.ComponentModel.DataAnnotations;
namespace OnlineTutor2.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string TeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual ApplicationUser Teacher { get; set; }
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<RegularTest> RegularTests { get; set; } = new List<RegularTest>();
        public virtual ICollection<SpellingTest> SpellingTests { get; set; } = new List<SpellingTest>();
        public virtual ICollection<PunctuationTest> PunctuationTests { get; set; } = new List<PunctuationTest>();
        public virtual ICollection<OrthoeopyTest> OrthoeopyTests { get; set; } = new List<OrthoeopyTest>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
