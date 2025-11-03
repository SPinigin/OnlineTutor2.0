using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = "";

        [StringLength(450)]
        public string? EntityId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ApplicationUser? User { get; set; }
    }

    // Типы действий
    public static class AuditActions
    {
        // Пользователи
        public const string UserCreated = "User Created";
        public const string UserUpdated = "User Updated";
        public const string UserDeleted = "User Deleted";
        public const string UserActivated = "User Activated";
        public const string UserDeactivated = "User Deactivated";

        // Учителя
        public const string TeacherApproved = "Teacher Approved";
        public const string TeacherRejected = "Teacher Rejected";

        // Классы
        public const string ClassCreated = "Class Created";
        public const string ClassUpdated = "Class Updated";
        public const string ClassDeleted = "Class Deleted";

        // Тесты
        public const string TestCreated = "Test Created";
        public const string TestUpdated = "Test Updated";
        public const string TestDeleted = "Test Deleted";

        // Результаты
        public const string ResultDeleted = "Result Deleted";
        public const string AllResultsCleared = "All Results Cleared";

        // Система
        public const string SystemBackup = "System Backup";
        public const string SystemRestore = "System Restore";
        public const string SettingsChanged = "Settings Changed";

        // Вход
        public const string AdminLogin = "Admin Login";
        public const string AdminLogout = "Admin Logout";
    }

    // Типы сущностей
    public static class AuditEntityTypes
    {
        public const string User = "User";
        public const string Student = "Student";
        public const string Teacher = "Teacher";
        public const string Class = "Class";
        public const string SpellingTest = "SpellingTest";
        public const string RegularTest = "RegularTest";
        public const string PunctuationTest = "PunctuationTest";
        public const string OrthoeopyTest = "OrthoeopyTest";
        public const string TestResult = "TestResult";
        public const string System = "System";
    }
}
