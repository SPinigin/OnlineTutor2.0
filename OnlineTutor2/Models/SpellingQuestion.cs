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
        public string WordWithGap { get; set; } // Слово с пропуском (например: "Прол…тает")

        [Required]
        [StringLength(10)]
        public string CorrectLetter { get; set; } // Правильная буква (например: "е")

        [Required]
        [StringLength(200)]
        public string FullWord { get; set; } // Полное слово (например: "пролетает")

        [StringLength(1000)]
        public string? Hint { get; set; } // Подсказка

        public int OrderIndex { get; set; } // Порядок вопроса в тесте

        public int Points { get; set; } = 1; // Баллы за правильный ответ

        // Навигационные свойства
        public virtual SpellingTest SpellingTest { get; set; }
        public virtual ICollection<SpellingAnswer> StudentAnswers { get; set; } = new List<SpellingAnswer>();
    }
}
