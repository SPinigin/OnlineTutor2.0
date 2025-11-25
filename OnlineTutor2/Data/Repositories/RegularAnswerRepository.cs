using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с ответами на классические тесты
    /// </summary>
    public class RegularAnswerRepository : BaseRepository<RegularAnswer>, IRegularAnswerRepository
    {
        public RegularAnswerRepository(IDatabaseConnection db) : base(db, "RegularAnswers")
        {
        }

        public async Task<List<RegularAnswer>> GetByTestResultIdAsync(int testResultId)
        {
            var sql = "SELECT * FROM RegularAnswers WHERE TestResultId = @TestResultId";
            return await _db.QueryAsync<RegularAnswer>(sql, new { TestResultId = testResultId });
        }

        public async Task<List<RegularAnswer>> GetByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT * FROM RegularAnswers WHERE QuestionId = @QuestionId";
            return await _db.QueryAsync<RegularAnswer>(sql, new { QuestionId = questionId });
        }

        public async Task<RegularAnswer?> GetByTestResultAndQuestionIdAsync(int testResultId, int questionId)
        {
            var sql = "SELECT * FROM RegularAnswers WHERE TestResultId = @TestResultId AND QuestionId = @QuestionId";
            return await _db.QueryFirstOrDefaultAsync<RegularAnswer>(sql, new { TestResultId = testResultId, QuestionId = questionId });
        }

        public async Task<int> GetCountByTestResultIdAsync(int testResultId)
        {
            var sql = "SELECT COUNT(*) FROM RegularAnswers WHERE TestResultId = @TestResultId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestResultId = testResultId });
            return result ?? 0;
        }

        public override async Task<int> CreateAsync(RegularAnswer entity)
        {
            var sql = @"
                INSERT INTO RegularAnswers (
                    TestResultId, QuestionId, SelectedOptionIds, StudentAnswer, 
                    IsCorrect, Points, AnsweredAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @TestResultId, @QuestionId, @SelectedOptionIds, @StudentAnswer,
                    @IsCorrect, @Points, @AnsweredAt
                )";
            
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.TestResultId,
                entity.QuestionId,
                entity.SelectedOptionIds,
                entity.StudentAnswer,
                entity.IsCorrect,
                entity.Points,
                entity.AnsweredAt
            });
            return id;
        }

        public override async Task<int> UpdateAsync(RegularAnswer entity)
        {
            var sql = @"
                UPDATE RegularAnswers 
                SET SelectedOptionIds = @SelectedOptionIds,
                    StudentAnswer = @StudentAnswer,
                    IsCorrect = @IsCorrect,
                    Points = @Points,
                    AnsweredAt = @AnsweredAt
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.SelectedOptionIds,
                entity.StudentAnswer,
                entity.IsCorrect,
                entity.Points,
                entity.AnsweredAt
            });
        }

        public async Task<int> GetTotalCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM RegularAnswers WHERE QuestionId = @QuestionId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<int> GetCorrectCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM RegularAnswers WHERE QuestionId = @QuestionId AND IsCorrect = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5)
        {
            // Для Regular тестов ошибки - это неправильные выбранные опции
            var sql = $@"
                SELECT TOP {topCount} 
                    COALESCE(SelectedOptionIds, '') as IncorrectAnswer,
                    COUNT(*) as Count
                FROM RegularAnswers 
                WHERE QuestionId = @QuestionId 
                    AND IsCorrect = 0 
                    AND SelectedOptionIds IS NOT NULL
                GROUP BY SelectedOptionIds
                ORDER BY COUNT(*) DESC";
            
            var mistakes = await _db.QueryAsync<dynamic>(sql, new { QuestionId = questionId });
            return mistakes.Select(m => new CommonMistakeInfo
            {
                IncorrectAnswer = m.IncorrectAnswer?.ToString() ?? "",
                Count = (int)m.Count
            }).ToList();
        }
    }
}

