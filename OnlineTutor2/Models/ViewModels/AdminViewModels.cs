using OnlineTutor2.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSpellingTests { get; set; }
        public int TotalRegularTests { get; set; }
        public int TotalTestResults { get; set; }
        public int PendingTeachers { get; set; }

        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
        public List<SpellingTest> RecentTests { get; set; } = new List<SpellingTest>();

        // Вычисляемые свойства
        public int TotalTests => TotalSpellingTests + TotalRegularTests;
        public double TeacherApprovalRate => TotalTeachers > 0 ? Math.Round((double)(TotalTeachers - PendingTeachers) / TotalTeachers * 100, 1) : 0;
    }

    public class AdminUserViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

        public string RolesDisplay => string.Join(", ", Roles);
        public string PrimaryRole => Roles.FirstOrDefault() ?? "Не назначена";

        public bool IsTeacher => Roles.Contains(ApplicationRoles.Teacher);
        public bool IsStudent => Roles.Contains(ApplicationRoles.Student);
        public bool IsAdmin => Roles.Contains(ApplicationRoles.Admin);
    }

    public class AdminUserDetailsViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }

        // Статистика для студента
        public int TestsCompleted { get; set; }
        public double AverageScore { get; set; }
        public DateTime? LastTestDate { get; set; }

        // Статистика для учителя
        public int ClassesCreated { get; set; }
        public int TestsCreated { get; set; }
        public int StudentsManaged { get; set; }
    }

    // Модель теста для админа
    public class AdminTestViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string TeacherName { get; set; }
        public string ClassName { get; set; }
        public int QuestionsCount { get; set; }
        public int ResultsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string ControllerName { get; set; }

        public string CreatedAtFormatted => CreatedAt.ToString("dd.MM.yyyy");
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Активный" : "Неактивный";
    }

    // Модель результата теста для админа
    public class AdminTestResultViewModel
    {
        public int Id { get; set; }
        public string TestTitle { get; set; }
        public string TestType { get; set; }
        public string StudentName { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
        public string ResultType { get; set; }
        public string StartedAtFormatted => StartedAt.ToString("dd.MM.yyyy HH:mm");
        public string CompletedAtFormatted => CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Не завершен";
        public string StatusBadge => IsCompleted ? "success" : "warning";
        public string StatusText => IsCompleted ? "Завершен" : "В процессе";
        public string ScoreDisplay => $"{Score}/{MaxScore} ({Percentage:F1}%)";
        public string PercentageBadge => Percentage >= 80 ? "success" : Percentage >= 60 ? "warning" : "danger";
    }

    // Системная информация
    public class AdminSystemInfoViewModel
    {
        public DatabaseInfoViewModel DatabaseInfo { get; set; } = new DatabaseInfoViewModel();
        public ServerInfoViewModel ServerInfo { get; set; } = new ServerInfoViewModel();
    }

    public class DatabaseInfoViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSpellingTests { get; set; }
        public int TotalRegularTests { get; set; }
        public int TotalSpellingQuestions { get; set; }
        public int TotalRegularQuestions { get; set; }
        public int TotalSpellingResults { get; set; }
        public int TotalRegularResults { get; set; }
        public int TotalMaterials { get; set; }

        // Вычисляемые свойства
        public int TotalTests => TotalSpellingTests + TotalRegularTests;
        public int TotalQuestions => TotalSpellingQuestions + TotalRegularQuestions;
        public int TotalResults => TotalSpellingResults + TotalRegularResults;
    }

    public class ServerInfoViewModel
    {
        public string Environment { get; set; } = "";
        public string RuntimeVersion { get; set; } = "";
        public DateTime ServerTime { get; set; } = DateTime.Now;
        public long MemoryUsage { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    // Модель для массовых операций
    public class AdminBulkActionViewModel
    {
        public string Action { get; set; } = ""; // "delete", "activate", "deactivate"
        public List<int> SelectedIds { get; set; } = new List<int>();
        public string EntityType { get; set; } = ""; // "users", "tests", "results"
    }

    // Модель для создания пользователя админом
    public class AdminCreateUserViewModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Дата рождения обязательна")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime DateOfBirth { get; set; }

        [Phone]
        [Display(Name = "Номер телефона")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Выберите роль")]
        [Display(Name = "Роль")]
        public string Role { get; set; } = "";

        [Display(Name = "Активный")]
        public bool IsActive { get; set; } = true;

        // Поля для студента
        [StringLength(200)]
        [Display(Name = "Школа")]
        public string? School { get; set; }

        [Range(1, 11)]
        [Display(Name = "Класс в школе")]
        public int? Grade { get; set; }

        [Display(Name = "Назначить в класс")]
        public int? ClassId { get; set; }

        // Поля для учителя
        [StringLength(100)]
        [Display(Name = "Предмет")]
        public string? Subject { get; set; }

        [StringLength(500)]
        [Display(Name = "Образование")]
        public string? Education { get; set; }

        [Range(0, 50)]
        [Display(Name = "Опыт работы (лет)")]
        public int? Experience { get; set; }

        [Display(Name = "Одобрен")]
        public bool IsApproved { get; set; } = true;
    }

    // Модель для редактирования пользователя админом
    public class AdminEditUserViewModel
    {
        public string Id { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public bool IsActive { get; set; }

        public List<string> CurrentRoles { get; set; } = new List<string>();
        public List<string> SelectedRoles { get; set; } = new List<string>();

        // Дополнительные поля профиля
        public string? School { get; set; }
        public int? Grade { get; set; }
        public int? ClassId { get; set; }
        public string? Subject { get; set; }
        public string? Education { get; set; }
        public int? Experience { get; set; }
        public bool IsApproved { get; set; }
    }

    // Модель фильтров для админ-панели
    public class AdminFilterViewModel
    {
        public string? SearchString { get; set; }
        public string? RoleFilter { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
