using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateClassViewModel
    {
        [Required(ErrorMessage = "Название класса обязательно")]
        [StringLength(100, ErrorMessage = "Название не должно превышать 100 символов")]
        [Display(Name = "Название класса")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }
    }
}
