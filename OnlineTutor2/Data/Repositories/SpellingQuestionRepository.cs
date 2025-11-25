using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class SpellingQuestionRepository : BaseRepository<SpellingQuestion>, ISpellingQuestionRepository
    {
        public SpellingQuestionRepository(IDatabaseConnection db) : base(db, "SpellingQuestions")
        {
        }

        public async Task<List<SpellingQuestion>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM SpellingQuestions WHERE SpellingTestId = @TestId";
            return await _db.QueryAsync<SpellingQuestion>(sql, new { TestId = testId });
        }

        public async Task<List<SpellingQuestion>> GetByTestIdOrderedAsync(int testId)
        {
            var sql = "SELECT * FROM SpellingQuestions WHERE SpellingTestId = @TestId ORDER BY OrderIndex";
            return await _db.QueryAsync<SpellingQuestion>(sql, new { TestId = testId });
        }

        public override async Task<int> CreateAsync(SpellingQuestion entity)
        {
            var sql = @"
                INSERT INTO SpellingQuestions (
                    SpellingTestId, WordWithGap, CorrectLetter, FullWord, Hint, OrderIndex, Points
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @SpellingTestId, @WordWithGap, @CorrectLetter, @FullWord, @Hint, @OrderIndex, @Points
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(SpellingQuestion entity)
        {
            var sql = @"
                UPDATE SpellingQuestions 
                SET WordWithGap = @WordWithGap, CorrectLetter = @CorrectLetter, 
                    FullWord = @FullWord, Hint = @Hint, OrderIndex = @OrderIndex, Points = @Points
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingQuestions WHERE SpellingTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }
    }
}




