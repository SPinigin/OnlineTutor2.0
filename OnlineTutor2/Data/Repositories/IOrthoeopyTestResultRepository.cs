using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IOrthoeopyTestResultRepository : IRepository<OrthoeopyTestResult>
    {
        Task<List<OrthoeopyTestResult>> GetByTestIdAsync(int testId);
        Task<List<OrthoeopyTestResult>> GetByStudentIdAsync(int studentId);
        Task<List<OrthoeopyTestResult>> GetCompletedByStudentIdAsync(int studentId);
        Task<List<OrthoeopyTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId);
        Task<OrthoeopyTestResult?> GetByIdWithDetailsAsync(int id, string teacherId);
        Task<int> GetCountByTestIdAsync(int testId);
        Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId);
        Task<int> GetTotalScoreByStudentIdAsync(int studentId);
        Task<double> GetAveragePercentageByStudentIdAsync(int studentId);
        
        // Analytics methods
        Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId);
        Task<int> GetDistinctStudentsCountByTestIdAsync(int testId);
        Task<int> GetCompletedStudentsCountByTestIdAsync(int testId);
        Task<int> GetInProgressStudentsCountByTestIdAsync(int testId);
        Task<List<OrthoeopyTestResult>> GetCompletedByTestIdAsync(int testId);
        Task<List<OrthoeopyTestResult>> GetInProgressByTestIdAsync(int testId);
        Task<double> GetAverageScoreByTestIdAsync(int testId);
        Task<double> GetAveragePercentageByTestIdAsync(int testId);
        Task<int> GetHighestScoreByTestIdAsync(int testId);
        Task<int> GetLowestScoreByTestIdAsync(int testId);
        Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId);
        Task<DateTime?> GetLastCompletionByTestIdAsync(int testId);
        Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId);
        Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId);
        Task<List<OrthoeopyTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId);
        Task<List<OrthoeopyTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId);
        Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId);
        Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId);
        Task<OrthoeopyTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId);
        Task<OrthoeopyTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId);
        Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId);
    }
}

