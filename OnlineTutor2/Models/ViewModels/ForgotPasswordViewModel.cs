using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
