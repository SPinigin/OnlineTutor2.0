using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Question
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
        public virtual Test Test { get; set; }
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }

    public enum QuestionType
    {
        MultipleChoice = 1,
        SingleChoice = 2,
        TrueFalse = 3,
        Text = 4,
        Essay = 5
    }
}
