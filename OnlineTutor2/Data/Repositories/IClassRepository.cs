using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с классами
    /// </summary>
    public interface IClassRepository : IRepository<Class>
    {
        Task<List<Class>> GetByTeacherIdAsync(string teacherId);
        Task<Class?> GetWithStudentsAsync(int id);
        Task<Class?> GetWithTeacherAsync(int id);
        Task<List<Class>> GetAllWithTeacherAsync();
        Task<int> GetTestCountAsync(int classId);
        Task<List<Student>> GetStudentsByClassIdsAsync(List<int> classIds);
    }
}

