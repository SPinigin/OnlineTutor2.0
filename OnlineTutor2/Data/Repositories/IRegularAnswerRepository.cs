using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с ответами на классические тесты
    /// </summary>
    public interface IRegularAnswerRepository : IRepository<RegularAnswer>
    {
        Task<List<RegularAnswer>> GetByTestResultIdAsync(int testResultId);
        Task<List<RegularAnswer>> GetByQuestionIdAsync(int questionId);
        Task<RegularAnswer?> GetByTestResultAndQuestionIdAsync(int testResultId, int questionId);
        Task<int> GetCountByTestResultIdAsync(int testResultId);
        Task<int> GetTotalCountByQuestionIdAsync(int questionId);
        Task<int> GetCorrectCountByQuestionIdAsync(int questionId);
        Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5);
    }
}

