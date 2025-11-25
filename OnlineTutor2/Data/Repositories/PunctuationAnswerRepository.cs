using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class PunctuationAnswerRepository : BaseRepository<PunctuationAnswer>, IPunctuationAnswerRepository
    {
        public PunctuationAnswerRepository(IDatabaseConnection db) : base(db, "PunctuationAnswers")
        {
        }

        public async Task<List<PunctuationAnswer>> GetByTestResultIdAsync(int testResultId)
        {
            var sql = "SELECT * FROM PunctuationAnswers WHERE PunctuationTestResultId = @TestResultId";
            return await _db.QueryAsync<PunctuationAnswer>(sql, new { TestResultId = testResultId });
        }

        public async Task<List<PunctuationAnswer>> GetByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT * FROM PunctuationAnswers WHERE PunctuationQuestionId = @QuestionId";
            return await _db.QueryAsync<PunctuationAnswer>(sql, new { QuestionId = questionId });
        }

        public override async Task<int> CreateAsync(PunctuationAnswer entity)
        {
            var sql = @"
                INSERT INTO PunctuationAnswers (
                    PunctuationTestResultId, PunctuationQuestionId, StudentAnswer, IsCorrect, Points, AnsweredAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @PunctuationTestResultId, @PunctuationQuestionId, @StudentAnswer, @IsCorrect, @Points, @AnsweredAt
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(PunctuationAnswer entity)
        {
            var sql = @"
                UPDATE PunctuationAnswers 
                SET StudentAnswer = @StudentAnswer, IsCorrect = @IsCorrect, Points = @Points
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetTotalCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationAnswers WHERE PunctuationQuestionId = @QuestionId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<int> GetCorrectCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationAnswers WHERE PunctuationQuestionId = @QuestionId AND IsCorrect = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5)
        {
            var sql = $@"
                SELECT TOP {topCount} 
                    LOWER(LTRIM(RTRIM(StudentAnswer))) as IncorrectAnswer,
                    COUNT(*) as Count
                FROM PunctuationAnswers 
                WHERE PunctuationQuestionId = @QuestionId 
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

