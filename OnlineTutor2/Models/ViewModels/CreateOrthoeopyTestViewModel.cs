using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateOrthoeopyTestViewModel
    {
        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        [Display(Name = "Название теста")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Класс")]
        public int? ClassId { get; set; }

        [Required]
        [Range(5, 300)]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; } = 30;

        [Required]
        [Range(1, 10)]
        [Display(Name = "Максимальное количество попыток")]
        public int MaxAttempts { get; set; } = 3;

        [Display(Name = "Дата начала")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Показывать подсказки")]
        public bool ShowHints { get; set; } = true;

        [Display(Name = "Показывать правильные ответы")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "Тест активен")]
        public bool IsActive { get; set; } = true;
    }
}
