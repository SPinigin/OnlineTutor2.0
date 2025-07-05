using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class SpellingQuestion
    {
        public int Id { get; set; }

        [Required]
        public int SpellingTestId { get; set; }

        [Required]
        [StringLength(200)]
        public string WordWithGap { get; set; }

        [Required]
        [StringLength(10)]
        public string CorrectLetter { get; set; }

        [Required]
        [StringLength(200)]
        public string FullWord { get; set; }

        [StringLength(1000)]
        public string? Hint { get; set; }

        public int OrderIndex { get; set; }

        public int Points { get; set; } = 1;

        // Навигационные свойства
        public virtual SpellingTest SpellingTest { get; set; }
        public virtual ICollection<SpellingAnswer> StudentAnswers { get; set; } = new List<SpellingAnswer>();
    }
}
