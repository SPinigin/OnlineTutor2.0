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

        [Display(Name = "Назначить в онлайн-класс")]
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

        [Display(Name = "Назначить в онлайн-класс")]
        public int? ClassId { get; set; }

        [StringLength(20)]
        [Display(Name = "Номер ученика")]
        public string? StudentNumber { get; set; }
    }

    public class ImportStudentsViewModel
    {
        [Required(ErrorMessage = "Выберите файл для импорта")]
        [Display(Name = "Excel файл")]
        public IFormFile ExcelFile { get; set; }

        [Display(Name = "Назначить в онлайн-класс")]
        public int? ClassId { get; set; }

        [Display(Name = "Создать пароли автоматически")]
        public bool AutoGeneratePasswords { get; set; } = true;

        [Display(Name = "Пароль по умолчанию")]
        [StringLength(100, MinimumLength = 6)]
        public string? DefaultPassword { get; set; } = "Student123";
    }
    public class ImportStudentRow
    {
        public int RowNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? School { get; set; }
        public int? Grade { get; set; }
        public string? Password { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsValid => !Errors.Any();
    }

    public class ImportResultViewModel
    {
        public int TotalRows { get; set; }
        public int SuccessfulImports { get; set; }
        public int FailedImports { get; set; }
        public List<ImportStudentRow> FailedRows { get; set; } = new List<ImportStudentRow>();
        public List<ImportStudentRow> SuccessfulRows { get; set; } = new List<ImportStudentRow>();
    }
}
