using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class RegularQuestionOption
    {
        public int Id { get; set; }

        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Текст варианта ответа обязателен")]
        [StringLength(500, ErrorMessage = "Текст не может превышать 500 символов")]
        [Display(Name = "Текст варианта ответа")]
        public string Text { get; set; } = string.Empty;

        [Display(Name = "Это правильный ответ")]
        public bool IsCorrect { get; set; } = false;

        [Display(Name = "Порядковый номер")]
        public int OrderIndex { get; set; }

        [StringLength(500, ErrorMessage = "Объяснение не может превышать 500 символов")]
        [Display(Name = "Объяснение (почему этот вариант верный/неверный)")]
        public string? Explanation { get; set; }

        // Навигационное свойство
        public virtual RegularQuestion Question { get; set; } = null!;
    }
}
