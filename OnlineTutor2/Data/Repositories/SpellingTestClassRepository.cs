using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class SpellingTestClassRepository : BaseRepository<SpellingTestClass>, ISpellingTestClassRepository
    {
        public SpellingTestClassRepository(IDatabaseConnection db) : base(db, "SpellingTestClasses")
        {
        }

        public async Task<List<SpellingTestClass>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM SpellingTestClasses WHERE SpellingTestId = @TestId";
            return await _db.QueryAsync<SpellingTestClass>(sql, new { TestId = testId });
        }

        public async Task<List<SpellingTestClass>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM SpellingTestClasses WHERE ClassId = @ClassId";
            return await _db.QueryAsync<SpellingTestClass>(sql, new { ClassId = classId });
        }

        public async Task<int> DeleteByTestIdAsync(int testId)
        {
            var sql = "DELETE FROM SpellingTestClasses WHERE SpellingTestId = @TestId";
            return await _db.ExecuteAsync(sql, new { TestId = testId });
        }

        public override async Task<int> CreateAsync(SpellingTestClass entity)
        {
            var sql = @"
                INSERT INTO SpellingTestClasses (SpellingTestId, ClassId, AssignedAt)
                OUTPUT INSERTED.Id
                VALUES (@SpellingTestId, @ClassId, @AssignedAt)";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public async Task<List<int>> GetClassIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT ClassId AS Id FROM SpellingTestClasses WHERE SpellingTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }
    }
}

