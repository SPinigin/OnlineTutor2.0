using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IPunctuationAnswerRepository : IRepository<PunctuationAnswer>
    {
        Task<List<PunctuationAnswer>> GetByTestResultIdAsync(int testResultId);
        Task<List<PunctuationAnswer>> GetByQuestionIdAsync(int questionId);
        Task<int> GetTotalCountByQuestionIdAsync(int questionId);
        Task<int> GetCorrectCountByQuestionIdAsync(int questionId);
        Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5);
    }
}

