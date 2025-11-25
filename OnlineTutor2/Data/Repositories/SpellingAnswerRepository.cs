using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class SpellingAnswerRepository : BaseRepository<SpellingAnswer>, ISpellingAnswerRepository
    {
        public SpellingAnswerRepository(IDatabaseConnection db) : base(db, "SpellingAnswers")
        {
        }

        public async Task<List<SpellingAnswer>> GetByTestResultIdAsync(int testResultId)
        {
            var sql = "SELECT * FROM SpellingAnswers WHERE SpellingTestResultId = @TestResultId";
            return await _db.QueryAsync<SpellingAnswer>(sql, new { TestResultId = testResultId });
        }

        public async Task<List<SpellingAnswer>> GetByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT * FROM SpellingAnswers WHERE SpellingQuestionId = @QuestionId";
            return await _db.QueryAsync<SpellingAnswer>(sql, new { QuestionId = questionId });
        }

        public override async Task<int> CreateAsync(SpellingAnswer entity)
        {
            var sql = @"
                INSERT INTO SpellingAnswers (
                    SpellingTestResultId, SpellingQuestionId, StudentAnswer, IsCorrect, Points, AnsweredAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @SpellingTestResultId, @SpellingQuestionId, @StudentAnswer, @IsCorrect, @Points, @AnsweredAt
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(SpellingAnswer entity)
        {
            var sql = @"
                UPDATE SpellingAnswers 
                SET StudentAnswer = @StudentAnswer, IsCorrect = @IsCorrect, Points = @Points
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetTotalCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingAnswers WHERE SpellingQuestionId = @QuestionId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<int> GetCorrectCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingAnswers WHERE SpellingQuestionId = @QuestionId AND IsCorrect = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5)
        {
            var sql = $@"
                SELECT TOP {topCount} 
                    LOWER(LTRIM(RTRIM(StudentAnswer))) as IncorrectAnswer,
                    COUNT(*) as Count
                FROM SpellingAnswers 
                WHERE SpellingQuestionId = @QuestionId 
                    AND IsCorrect = 0 
                    AND StudentAnswer IS NOT NULL 
                    AND LTRIM(RTRIM(StudentAnswer)) != ''
                GROUP BY LOWER(LTRIM(RTRIM(StudentAnswer)))
                ORDER BY COUNT(*) DESC";
            
            var mistakes = await _db.QueryAsync<dynamic>(sql, new { QuestionId = questionId });
            return mistakes.Select(m => new CommonMistakeInfo
            {
                IncorrectAnswer = (string)m.IncorrectAnswer,
                Count = (int)m.Count
            }).ToList();
        }
    }
}

