using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class OrthoeopyQuestionRepository : BaseRepository<OrthoeopyQuestion>, IOrthoeopyQuestionRepository
    {
        public OrthoeopyQuestionRepository(IDatabaseConnection db) : base(db, "OrthoeopyQuestions")
        {
        }

        public async Task<List<OrthoeopyQuestion>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM OrthoeopyQuestions WHERE OrthoeopyTestId = @TestId";
            return await _db.QueryAsync<OrthoeopyQuestion>(sql, new { TestId = testId });
        }

        public async Task<List<OrthoeopyQuestion>> GetByTestIdOrderedAsync(int testId)
        {
            var sql = "SELECT * FROM OrthoeopyQuestions WHERE OrthoeopyTestId = @TestId ORDER BY OrderIndex";
            return await _db.QueryAsync<OrthoeopyQuestion>(sql, new { TestId = testId });
        }

        public override async Task<int> CreateAsync(OrthoeopyQuestion entity)
        {
            var sql = @"
                INSERT INTO OrthoeopyQuestions (
                    OrthoeopyTestId, Word, StressPosition, WordWithStress, Hint, 
                    WrongStressPositions, OrderIndex, Points
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @OrthoeopyTestId, @Word, @StressPosition, @WordWithStress, @Hint,
                    @WrongStressPositions, @OrderIndex, @Points
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(OrthoeopyQuestion entity)
        {
            var sql = @"
                UPDATE OrthoeopyQuestions 
                SET Word = @Word, StressPosition = @StressPosition, WordWithStress = @WordWithStress,
                    Hint = @Hint, WrongStressPositions = @WrongStressPositions, 
                    OrderIndex = @OrderIndex, Points = @Points
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyQuestions WHERE OrthoeopyTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }
    }
}




