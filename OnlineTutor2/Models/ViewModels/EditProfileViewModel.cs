using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Введите имя")]
        [StringLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введите фамилию")]
        [StringLength(50, ErrorMessage = "Фамилия не должна превышать 50 символов")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Неверный формат телефона")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Введите дату рождения")]
        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string CurrentEmail { get; set; }
    }
}
