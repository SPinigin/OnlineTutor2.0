using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularQuestion
    {
        public int Id { get; set; }

        [Required]
        public int TestId { get; set; }

        [Required]
        public string Text { get; set; }

        public QuestionType Type { get; set; }

        public int Points { get; set; } = 1;

        public int OrderIndex { get; set; }

        // Навигационные свойства
        public virtual RegularTest RegularTest { get; set; }
        public virtual ICollection<RegularAnswer> RegularAnswers { get; set; } = new List<RegularAnswer>();
    }

    public enum QuestionType
    {
        MultipleChoice = 1,
        SingleChoice = 2,
        TrueFalse = 3
    }
}
