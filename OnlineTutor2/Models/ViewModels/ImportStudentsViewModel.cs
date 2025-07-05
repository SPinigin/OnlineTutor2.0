using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class ImportStudentsViewModel
    {
        [Required(ErrorMessage = "Выберите файл для импорта")]
        [Display(Name = "Excel файл")]
        public IFormFile ExcelFile { get; set; }

        [Display(Name = "Назначить в класс")]
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
