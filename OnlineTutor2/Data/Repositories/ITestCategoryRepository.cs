using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ITestCategoryRepository : IRepository<TestCategory>
    {
        Task<List<TestCategory>> GetActiveAsync();
    }
}





