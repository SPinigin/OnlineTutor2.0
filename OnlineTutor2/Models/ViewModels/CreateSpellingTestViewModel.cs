using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateSpellingTestViewModel
    {
        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200)]
        [Display(Name = "Название теста")]
        public string Title { get; set; }

        [StringLength(1000)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Назначить классу")]
        public int? ClassId { get; set; }

        [Range(5, 180, ErrorMessage = "Время должно быть от 5 до 180 минут")]
        [Display(Name = "Время на выполнение (минут)")]
        public int TimeLimit { get; set; } = 30;

        [Range(1, 10, ErrorMessage = "Количество попыток должно быть от 1 до 10")]
        [Display(Name = "Максимальное количество попыток")]
        public int MaxAttempts { get; set; } = 1;

        [Display(Name = "Дата начала")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Показывать подсказки")]
        public bool ShowHints { get; set; } = true;

        [Display(Name = "Показывать правильные ответы после теста")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "Активный")]
        public bool IsActive { get; set; } = true;
    }
}
