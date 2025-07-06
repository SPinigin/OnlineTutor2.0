using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class PunctuationQuestion
    {
        public int Id { get; set; }

        [Required]
        public int PunctuationTestId { get; set; }

        [Required]
        [StringLength(1000)]
        public string SentenceWithNumbers { get; set; }

        [Required]
        [StringLength(50)]
        public string CorrectPositions { get; set; }

        [StringLength(1000)]
        public string? PlainSentence { get; set; }

        [StringLength(1000)]
        public string? Hint { get; set; }

        public int OrderIndex { get; set; }

        public int Points { get; set; } = 1;

        // Навигационные свойства
        public virtual PunctuationTest PunctuationTest { get; set; }
        public virtual ICollection<PunctuationAnswer> StudentAnswers { get; set; } = new List<PunctuationAnswer>();
    }
}