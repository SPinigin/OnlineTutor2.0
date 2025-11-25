using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IOrthoeopyAnswerRepository : IRepository<OrthoeopyAnswer>
    {
        Task<List<OrthoeopyAnswer>> GetByTestResultIdAsync(int testResultId);
        Task<List<OrthoeopyAnswer>> GetByQuestionIdAsync(int questionId);
        Task<int> GetTotalCountByQuestionIdAsync(int questionId);
        Task<int> GetCorrectCountByQuestionIdAsync(int questionId);
        Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5);
    }
}

