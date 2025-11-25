using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с вопросами классических тестов
    /// </summary>
    public class RegularQuestionRepository : BaseRepository<RegularQuestion>, IRegularQuestionRepository
    {
        public RegularQuestionRepository(IDatabaseConnection db) : base(db, "RegularQuestions")
        {
        }

        public async Task<List<RegularQuestion>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM RegularQuestions WHERE TestId = @TestId";
            return await _db.QueryAsync<RegularQuestion>(sql, new { TestId = testId });
        }

        public async Task<List<RegularQuestion>> GetByTestIdOrderedAsync(int testId)
        {
            var sql = "SELECT * FROM RegularQuestions WHERE TestId = @TestId ORDER BY OrderIndex";
            return await _db.QueryAsync<RegularQuestion>(sql, new { TestId = testId });
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM RegularQuestions WHERE TestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public override async Task<int> CreateAsync(RegularQuestion entity)
        {
            var sql = @"
                INSERT INTO RegularQuestions (
                    TestId, Text, Type, Points, OrderIndex, Hint, Explanation
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @TestId, @Text, @Type, @Points, @OrderIndex, @Hint, @Explanation
                )";
            
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.TestId,
                entity.Text,
                Type = (int)entity.Type,
                entity.Points,
                entity.OrderIndex,
                entity.Hint,
                entity.Explanation
            });
            return id;
        }

        public override async Task<int> UpdateAsync(RegularQuestion entity)
        {
            var sql = @"
                UPDATE RegularQuestions 
                SET Text = @Text,
                    Type = @Type,
                    Points = @Points,
                    OrderIndex = @OrderIndex,
                    Hint = @Hint,
                    Explanation = @Explanation
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Text,
                Type = (int)entity.Type,
                entity.Points,
                entity.OrderIndex,
                entity.Hint,
                entity.Explanation
            });
        }
    }
}





