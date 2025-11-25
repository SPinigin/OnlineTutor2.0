using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ISpellingTestResultRepository : IRepository<SpellingTestResult>
    {
        Task<List<SpellingTestResult>> GetByTestIdAsync(int testId);
        Task<List<SpellingTestResult>> GetByStudentIdAsync(int studentId);
        Task<List<SpellingTestResult>> GetCompletedByStudentIdAsync(int studentId);
        Task<List<SpellingTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId);
        Task<SpellingTestResult?> GetByIdWithDetailsAsync(int id, string teacherId);
        Task<int> GetCountByTestIdAsync(int testId);
        Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId);
        Task<int> GetTotalScoreByStudentIdAsync(int studentId);
        Task<double> GetAveragePercentageByStudentIdAsync(int studentId);
        
        // Analytics methods
        Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId);
        Task<int> GetDistinctStudentsCountByTestIdAsync(int testId);
        Task<int> GetCompletedStudentsCountByTestIdAsync(int testId);
        Task<int> GetInProgressStudentsCountByTestIdAsync(int testId);
        Task<List<SpellingTestResult>> GetCompletedByTestIdAsync(int testId);
        Task<List<SpellingTestResult>> GetInProgressByTestIdAsync(int testId);
        Task<double> GetAverageScoreByTestIdAsync(int testId);
        Task<double> GetAveragePercentageByTestIdAsync(int testId);
        Task<int> GetHighestScoreByTestIdAsync(int testId);
        Task<int> GetLowestScoreByTestIdAsync(int testId);
        Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId);
        Task<DateTime?> GetLastCompletionByTestIdAsync(int testId);
        Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId);
        Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId);
        Task<List<SpellingTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId);
        Task<List<SpellingTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId);
        Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId);
        Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId);
        Task<SpellingTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId);
        Task<SpellingTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId);
        Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId);
    }
}

