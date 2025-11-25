using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с вариантами ответов на вопросы классических тестов
    /// </summary>
    public class RegularQuestionOptionRepository : BaseRepository<RegularQuestionOption>, IRegularQuestionOptionRepository
    {
        public RegularQuestionOptionRepository(IDatabaseConnection db) : base(db, "RegularQuestionOptions")
        {
        }

        public async Task<List<RegularQuestionOption>> GetByQuestionIdAsync(int questionId)
        {
            var sql = "SELECT * FROM RegularQuestionOptions WHERE QuestionId = @QuestionId";
            return await _db.QueryAsync<RegularQuestionOption>(sql, new { QuestionId = questionId });
        }

        public async Task<List<RegularQuestionOption>> GetByQuestionIdOrderedAsync(int questionId)
        {
            var sql = "SELECT * FROM RegularQuestionOptions WHERE QuestionId = @QuestionId ORDER BY OrderIndex";
            return await _db.QueryAsync<RegularQuestionOption>(sql, new { QuestionId = questionId });
        }

        public async Task<int> DeleteByQuestionIdAsync(int questionId)
        {
            var sql = "DELETE FROM RegularQuestionOptions WHERE QuestionId = @QuestionId";
            return await _db.ExecuteAsync(sql, new { QuestionId = questionId });
        }

        public override async Task<int> CreateAsync(RegularQuestionOption entity)
        {
            var sql = @"
                INSERT INTO RegularQuestionOptions (
                    QuestionId, Text, IsCorrect, OrderIndex, Explanation
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @QuestionId, @Text, @IsCorrect, @OrderIndex, @Explanation
                )";
            
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.QuestionId,
                entity.Text,
                entity.IsCorrect,
                entity.OrderIndex,
                entity.Explanation
            });
            return id;
        }

        public override async Task<int> UpdateAsync(RegularQuestionOption entity)
        {
            var sql = @"
                UPDATE RegularQuestionOptions 
                SET Text = @Text,
                    IsCorrect = @IsCorrect,
                    OrderIndex = @OrderIndex,
                    Explanation = @Explanation
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Text,
                entity.IsCorrect,
                entity.OrderIndex,
                entity.Explanation
            });
        }
    }
}





