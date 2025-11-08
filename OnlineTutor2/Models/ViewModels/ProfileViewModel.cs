using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }

        // Статистика для студентов
        public int TotalTestsCompleted { get; set; }
        public double AverageScore { get; set; }
        public int TotalPointsEarned { get; set; }

        // Статистика для учителей
        public int TotalStudents { get; set; }
        public int TotalTests { get; set; }
        public int TotalClasses { get; set; }
    }
}
