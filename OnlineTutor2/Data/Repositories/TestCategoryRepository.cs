using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class TestCategoryRepository : BaseRepository<TestCategory>, ITestCategoryRepository
    {
        public TestCategoryRepository(IDatabaseConnection db) : base(db, "TestCategories")
        {
        }

        public async Task<List<TestCategory>> GetActiveAsync()
        {
            var sql = "SELECT * FROM TestCategories WHERE IsActive = 1 ORDER BY OrderIndex";
            return await _db.QueryAsync<TestCategory>(sql);
        }

        public override async Task<int> CreateAsync(TestCategory entity)
        {
            var sql = @"
                INSERT INTO TestCategories (
                    Name, Description, IconClass, ColorClass, OrderIndex, IsActive
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Name, @Description, @IconClass, @ColorClass, @OrderIndex, @IsActive
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(TestCategory entity)
        {
            var sql = @"
                UPDATE TestCategories 
                SET Name = @Name, Description = @Description, IconClass = @IconClass,
                    ColorClass = @ColorClass, OrderIndex = @OrderIndex, IsActive = @IsActive
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }
    }
}





