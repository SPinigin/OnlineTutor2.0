using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateStudentViewModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Дата рождения обязательна")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime DateOfBirth { get; set; }

        [Phone(ErrorMessage = "Неверный формат номера телефона")]
        [Display(Name = "Номер телефона")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Школа")]
        public string? School { get; set; }

        [Range(1, 11, ErrorMessage = "Класс должен быть от 1 до 11")]
        [Display(Name = "Класс в школе")]
        public int? Grade { get; set; }

        [Display(Name = "Назначить в класс")]
        public int? ClassId { get; set; }
    }

    public class EditStudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Неверный формат номера телефона")]
        [Display(Name = "Номер телефона")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Дата рождения обязательна")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime DateOfBirth { get; set; }

        [StringLength(200)]
        [Display(Name = "Школа")]
        public string? School { get; set; }

        [Range(1, 11, ErrorMessage = "Класс должен быть от 1 до 11")]
        [Display(Name = "Класс в школе")]
        public int? Grade { get; set; }

        [Display(Name = "Назначить в класс")]
        public int? ClassId { get; set; }

        [StringLength(20)]
        [Display(Name = "Номер ученика")]
        public string? StudentNumber { get; set; }
    }
}
