using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class OrthoeopyTestClassRepository : BaseRepository<OrthoeopyTestClass>, IOrthoeopyTestClassRepository
    {
        public OrthoeopyTestClassRepository(IDatabaseConnection db) : base(db, "OrthoeopyTestClasses")
        {
        }

        public async Task<List<OrthoeopyTestClass>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM OrthoeopyTestClasses WHERE OrthoeopyTestId = @TestId";
            return await _db.QueryAsync<OrthoeopyTestClass>(sql, new { TestId = testId });
        }

        public async Task<List<OrthoeopyTestClass>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM OrthoeopyTestClasses WHERE ClassId = @ClassId";
            return await _db.QueryAsync<OrthoeopyTestClass>(sql, new { ClassId = classId });
        }

        public async Task<int> DeleteByTestIdAsync(int testId)
        {
            var sql = "DELETE FROM OrthoeopyTestClasses WHERE OrthoeopyTestId = @TestId";
            return await _db.ExecuteAsync(sql, new { TestId = testId });
        }

        public override async Task<int> CreateAsync(OrthoeopyTestClass entity)
        {
            var sql = @"
                INSERT INTO OrthoeopyTestClasses (OrthoeopyTestId, ClassId, AssignedAt)
                OUTPUT INSERTED.Id
                VALUES (@OrthoeopyTestId, @ClassId, @AssignedAt)";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public async Task<List<int>> GetClassIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT ClassId AS Id FROM OrthoeopyTestClasses WHERE OrthoeopyTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }
    }
}

