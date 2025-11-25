using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class PunctuationQuestionRepository : BaseRepository<PunctuationQuestion>, IPunctuationQuestionRepository
    {
        public PunctuationQuestionRepository(IDatabaseConnection db) : base(db, "PunctuationQuestions")
        {
        }

        public async Task<List<PunctuationQuestion>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM PunctuationQuestions WHERE PunctuationTestId = @TestId";
            return await _db.QueryAsync<PunctuationQuestion>(sql, new { TestId = testId });
        }

        public async Task<List<PunctuationQuestion>> GetByTestIdOrderedAsync(int testId)
        {
            var sql = "SELECT * FROM PunctuationQuestions WHERE PunctuationTestId = @TestId ORDER BY OrderIndex";
            return await _db.QueryAsync<PunctuationQuestion>(sql, new { TestId = testId });
        }

        public override async Task<int> CreateAsync(PunctuationQuestion entity)
        {
            var sql = @"
                INSERT INTO PunctuationQuestions (
                    PunctuationTestId, SentenceWithNumbers, CorrectPositions, PlainSentence, 
                    Hint, OrderIndex, Points
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @PunctuationTestId, @SentenceWithNumbers, @CorrectPositions, @PlainSentence,
                    @Hint, @OrderIndex, @Points
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(PunctuationQuestion entity)
        {
            var sql = @"
                UPDATE PunctuationQuestions 
                SET SentenceWithNumbers = @SentenceWithNumbers, CorrectPositions = @CorrectPositions,
                    PlainSentence = @PlainSentence, Hint = @Hint, OrderIndex = @OrderIndex, Points = @Points
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationQuestions WHERE PunctuationTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }
    }
}




