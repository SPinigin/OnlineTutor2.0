using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IMaterialRepository : IRepository<Material>
    {
        Task<List<Material>> GetByClassIdAsync(int classId);
        Task<List<Material>> GetByUploadedByIdAsync(string uploadedById);
        Task<List<Material>> GetActiveByClassIdAsync(int classId);
        Task<List<Material>> GetFilteredAsync(
            string? uploadedById,
            string? searchString = null,
            int? classFilter = null,
            MaterialType? typeFilter = null,
            string? sortOrder = null);
        Task<Material?> GetByIdWithClassAsync(int id, string uploadedById);
    }
}

