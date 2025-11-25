using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateTestAssignmentViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Название задания обязательно")]
        [StringLength(200, ErrorMessage = "Название не может превышать 200 символов")]
        [Display(Name = "Название задания")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Номер задания обязателен")]
        [Range(1, 1000, ErrorMessage = "Номер задания должен быть от 1 до 1000")]
        [Display(Name = "Номер задания")]
        public int AssignmentNumber { get; set; }

        [Required(ErrorMessage = "Выберите категорию тестов")]
        [Display(Name = "Категория тестов")]
        public int TestCategoryId { get; set; }

        [Display(Name = "Активно")]
        public bool IsActive { get; set; } = true;
    }
}




