using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string Text { get; set; }

        public bool IsCorrect { get; set; }

        public int OrderIndex { get; set; }

        public virtual Question Question { get; set; }
    }
}
