using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class OrthoeopyAnswerRepository : BaseRepository<OrthoeopyAnswer>, IOrthoeopyAnswerRepository
    {
        public OrthoeopyAnswerRepository(IDatabaseConnection db) : base(db, "OrthoeopyAnswers")
        {
        }

        public async Task<List<OrthoeopyAnswer>> GetByTestResultIdAsync(int testResultId)
        {
            var sql = "SELECT * FROM OrthoeopyAnswers WHERE OrthoeopyTestResultId = @TestResultId";
            return await _db.QueryAsync<OrthoeopyAnswer>(sql, new { TestResultId = testResultId });
        }

        public async Task<List<OrthoeopyAnswer>> GetByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT * FROM OrthoeopyAnswers WHERE OrthoeopyQuestionId = @QuestionId";
            return await _db.QueryAsync<OrthoeopyAnswer>(sql, new { QuestionId = questionId });
        }

        public override async Task<int> CreateAsync(OrthoeopyAnswer entity)
        {
            var sql = @"
                INSERT INTO OrthoeopyAnswers (
                    OrthoeopyTestResultId, OrthoeopyQuestionId, SelectedStressPosition, IsCorrect, Points, AnsweredAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @OrthoeopyTestResultId, @OrthoeopyQuestionId, @SelectedStressPosition, @IsCorrect, @Points, @AnsweredAt
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(OrthoeopyAnswer entity)
        {
            var sql = @"
                UPDATE OrthoeopyAnswers 
                SET SelectedStressPosition = @SelectedStressPosition, IsCorrect = @IsCorrect, Points = @Points
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetTotalCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyAnswers WHERE OrthoeopyQuestionId = @QuestionId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<int> GetCorrectCountByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyAnswers WHERE OrthoeopyQuestionId = @QuestionId AND IsCorrect = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { QuestionId = questionId });
            return result ?? 0;
        }

        public async Task<List<CommonMistakeInfo>> GetCommonMistakesByQuestionIdAsync(int questionId, int topCount = 5)
        {
            // Для орфоэпии ошибки - это неправильные позиции ударения
            var sql = $@"
                SELECT TOP {topCount} 
                    CAST(SelectedStressPosition AS VARCHAR) as IncorrectAnswer,
                    COUNT(*) as Count
                FROM OrthoeopyAnswers 
                WHERE OrthoeopyQuestionId = @QuestionId 
                    AND IsCorrect = 0 
                    AND SelectedStressPosition IS NOT NULL
                GROUP BY SelectedStressPosition
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

