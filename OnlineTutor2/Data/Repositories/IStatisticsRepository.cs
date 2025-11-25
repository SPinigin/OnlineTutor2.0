using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для получения статистики системы
    /// </summary>
    public interface IStatisticsRepository
    {
        // Общая статистика
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetTotalStudentsCountAsync();
        Task<int> GetTotalTeachersCountAsync();
        Task<int> GetTotalClassesCountAsync();
        Task<int> GetTotalSpellingTestsCountAsync();
        Task<int> GetTotalRegularTestsCountAsync();
        Task<int> GetTotalPunctuationTestsCountAsync();
        Task<int> GetTotalOrthoeopyTestsCountAsync();
        Task<int> GetTotalTestResultsCountAsync();
        Task<int> GetPendingTeachersCountAsync();

        // Последние записи
        Task<List<ApplicationUser>> GetRecentUsersAsync(int count = 5);
        Task<List<SpellingTest>> GetRecentSpellingTestsAsync(int count = 5);

        // Статистика по датам
        Task<Dictionary<DateTime, int>> GetUserRegistrationsByDateAsync(DateTime fromDate);
        Task<Dictionary<DateTime, int>> GetTestResultsByDateAsync(DateTime fromDate, string testType);
        Task<Dictionary<string, int>> GetTestsByTypeAsync();
        Task<Dictionary<string, double>> GetAverageScoresByTypeAsync();
        Task<Dictionary<string, int>> GetAdminActionsByTypeAsync(DateTime fromDate);
        Task<Dictionary<string, int>> GetActivityByDayOfWeekAsync(DateTime fromDate);
        Task<int> GetActiveUsersCountAsync();
        Task<int> GetInactiveUsersCountAsync();
        Task<Dictionary<string, int>> GetTopTeachersByTestsAsync(int count = 5);
        Task<Dictionary<string, int>> GetTopStudentsByResultsAsync(int count = 5);

        // Системная информация
        Task<int> GetTotalSpellingQuestionsCountAsync();
        Task<int> GetTotalRegularQuestionsCountAsync();
        Task<int> GetTotalSpellingResultsCountAsync();
        Task<int> GetTotalRegularResultsCountAsync();
        Task<int> GetTotalMaterialsCountAsync();
    }
}





