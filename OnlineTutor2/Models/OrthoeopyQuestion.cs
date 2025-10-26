using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class OrthoeopyQuestion
    {
        public int Id { get; set; }

        [Required]
        public int OrthoeopyTestId { get; set; }

        [Required]
        [StringLength(200)]
        public string Word { get; set; } = string.Empty;

        // Позиция ударного слога (начиная с 1)
        [Required]
        [Range(1, 20, ErrorMessage = "Позиция ударения должна быть от 1 до 20")]
        public int StressPosition { get; set; }

        // Слово с правильным ударением для отображения (например: догов́ор)
        [Required]
        [StringLength(200)]
        public string WordWithStress { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Hint { get; set; }

        // Для хранения неправильных вариантов (JSON массив позиций)
        [StringLength(100)]
        public string? WrongStressPositions { get; set; }

        public int OrderIndex { get; set; }
        public int Points { get; set; } = 1;

        // Навигационные свойства
        public virtual OrthoeopyTest OrthoeopyTest { get; set; } = null!;
        public virtual ICollection<OrthoeopyAnswer> StudentAnswers { get; set; } = new List<OrthoeopyAnswer>();
    }
}
