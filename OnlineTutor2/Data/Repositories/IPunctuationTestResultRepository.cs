using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IPunctuationTestResultRepository : IRepository<PunctuationTestResult>
    {
        Task<List<PunctuationTestResult>> GetByTestIdAsync(int testId);
        Task<List<PunctuationTestResult>> GetByStudentIdAsync(int studentId);
        Task<List<PunctuationTestResult>> GetCompletedByStudentIdAsync(int studentId);
        Task<List<PunctuationTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId);
        Task<PunctuationTestResult?> GetByIdWithDetailsAsync(int id, string teacherId);
        Task<int> GetCountByTestIdAsync(int testId);
        Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId);
        Task<int> GetTotalScoreByStudentIdAsync(int studentId);
        Task<double> GetAveragePercentageByStudentIdAsync(int studentId);
        
        // Analytics methods
        Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId);
        Task<int> GetDistinctStudentsCountByTestIdAsync(int testId);
        Task<int> GetCompletedStudentsCountByTestIdAsync(int testId);
        Task<int> GetInProgressStudentsCountByTestIdAsync(int testId);
        Task<List<PunctuationTestResult>> GetCompletedByTestIdAsync(int testId);
        Task<List<PunctuationTestResult>> GetInProgressByTestIdAsync(int testId);
        Task<double> GetAverageScoreByTestIdAsync(int testId);
        Task<double> GetAveragePercentageByTestIdAsync(int testId);
        Task<int> GetHighestScoreByTestIdAsync(int testId);
        Task<int> GetLowestScoreByTestIdAsync(int testId);
        Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId);
        Task<DateTime?> GetLastCompletionByTestIdAsync(int testId);
        Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId);
        Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId);
        Task<List<PunctuationTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId);
        Task<List<PunctuationTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId);
        Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId);
        Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId);
        Task<PunctuationTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId);
        Task<PunctuationTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId);
        Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId);
    }
}

