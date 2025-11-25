using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы со студентами
    /// </summary>
    public interface IStudentRepository : IRepository<Student>
    {
        Task<Student?> GetByUserIdAsync(string userId);
        Task<List<Student>> GetByClassIdAsync(int classId);
        Task<List<Student>> GetByTeacherIdAsync(string teacherId);
        Task<Student?> GetWithUserAsync(int id);
        Task<List<Student>> GetAllWithUserAsync();
        Task<(List<Student> Students, int TotalCount)> GetPaginatedAsync(
            List<int>? teacherClassIds = null,
            string? searchString = null,
            int? classFilter = null,
            string? sortOrder = null,
            int pageNumber = 1,
            int pageSize = 10);
        Task<List<string>> GetUserIdsInClassesAsync(List<int> classIds);
        Task<List<ApplicationUser>> GetAvailableStudentsAsync(
            HashSet<string> excludedUserIds,
            string? searchString = null);
        Task<string?> GetLastStudentNumberAsync(int year);
        Task<int> GetDistinctStudentCountByTeacherIdAsync(string teacherId);
    }
}

