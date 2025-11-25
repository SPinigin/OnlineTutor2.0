using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class RegularTestClassRepository : BaseRepository<RegularTestClass>, IRegularTestClassRepository
    {
        public RegularTestClassRepository(IDatabaseConnection db) : base(db, "RegularTestClasses")
        {
        }

        public async Task<List<RegularTestClass>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM RegularTestClasses WHERE RegularTestId = @TestId";
            return await _db.QueryAsync<RegularTestClass>(sql, new { TestId = testId });
        }

        public async Task<List<RegularTestClass>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM RegularTestClasses WHERE ClassId = @ClassId";
            return await _db.QueryAsync<RegularTestClass>(sql, new { ClassId = classId });
        }

        public async Task<int> DeleteByTestIdAsync(int testId)
        {
            var sql = "DELETE FROM RegularTestClasses WHERE RegularTestId = @TestId";
            return await _db.ExecuteAsync(sql, new { TestId = testId });
        }

        public async Task<int> DeleteByTestAndClassIdAsync(int testId, int classId)
        {
            var sql = "DELETE FROM RegularTestClasses WHERE RegularTestId = @TestId AND ClassId = @ClassId";
            return await _db.ExecuteAsync(sql, new { TestId = testId, ClassId = classId });
        }

        public override async Task<int> CreateAsync(RegularTestClass entity)
        {
            var sql = @"
                INSERT INTO RegularTestClasses (RegularTestId, ClassId, AssignedAt)
                OUTPUT INSERTED.Id
                VALUES (@RegularTestId, @ClassId, @AssignedAt)";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public async Task<List<int>> GetClassIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT ClassId AS Id FROM RegularTestClasses WHERE RegularTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }
    }
}

