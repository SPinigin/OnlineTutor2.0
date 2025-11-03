namespace OnlineTutor2.ViewModels
{
    public class AdminStatisticsViewModel
    {
        // Регистрации по дням (последние 30 дней)
        public Dictionary<DateTime, int> UserRegistrationsByDate { get; set; } = new();

        // Тесты по типам
        public Dictionary<string, int> TestsByType { get; set; } = new();

        // Результаты тестов по дням
        public Dictionary<DateTime, int> TestResultsByDate { get; set; } = new();

        // Средний балл по типам тестов
        public Dictionary<string, double> AverageScoresByType { get; set; } = new();

        // Действия администраторов по типам
        public Dictionary<string, int> AdminActionsByType { get; set; } = new();

        // Активность по дням недели
        public Dictionary<string, int> ActivityByDayOfWeek { get; set; } = new();

        // Количество активных/неактивных пользователей
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }

        // Топ учителей по количеству тестов
        public Dictionary<string, int> TopTeachersByTests { get; set; } = new();

        // Топ студентов по количеству пройденных тестов
        public Dictionary<string, int> TopStudentsByResults { get; set; } = new();
    }
}
