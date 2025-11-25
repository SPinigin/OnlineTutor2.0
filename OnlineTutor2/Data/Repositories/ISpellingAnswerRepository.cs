using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ISpellingAnswerRepository : IRepository<SpellingAnswer>
    {
        Task<List<SpellingAnswer>> GetByTestResultIdAsync(int testResultId);
        Task<List<SpellingAnswer>> GetByQuestionIdAsync(int questionId);
        Task<int> GetTotalCountByQuestionIdAsync(int questionId);
        Task<int> GetCorrectCountByQuestionIdAsync(int questionId);
        Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5);
    }
    
    public class CommonMistakeInfo
    {
        public string IncorrectAnswer { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

