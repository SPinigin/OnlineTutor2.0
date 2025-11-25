using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class PunctuationTestClassRepository : BaseRepository<PunctuationTestClass>, IPunctuationTestClassRepository
    {
        public PunctuationTestClassRepository(IDatabaseConnection db) : base(db, "PunctuationTestClasses")
        {
        }

        public async Task<List<PunctuationTestClass>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM PunctuationTestClasses WHERE PunctuationTestId = @TestId";
            return await _db.QueryAsync<PunctuationTestClass>(sql, new { TestId = testId });
        }

        public async Task<List<PunctuationTestClass>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM PunctuationTestClasses WHERE ClassId = @ClassId";
            return await _db.QueryAsync<PunctuationTestClass>(sql, new { ClassId = classId });
        }

        public async Task<int> DeleteByTestIdAsync(int testId)
        {
            var sql = "DELETE FROM PunctuationTestClasses WHERE PunctuationTestId = @TestId";
            return await _db.ExecuteAsync(sql, new { TestId = testId });
        }

        public override async Task<int> CreateAsync(PunctuationTestClass entity)
        {
            var sql = @"
                INSERT INTO PunctuationTestClasses (PunctuationTestId, ClassId, AssignedAt)
                OUTPUT INSERTED.Id
                VALUES (@PunctuationTestId, @ClassId, @AssignedAt)";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public async Task<List<int>> GetClassIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT ClassId AS Id FROM PunctuationTestClasses WHERE PunctuationTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }
    }
}

