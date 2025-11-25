using System.Dynamic;
using System.Linq;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с классами
    /// </summary>
    public class ClassRepository : BaseRepository<Class>, IClassRepository
    {
        public ClassRepository(IDatabaseConnection db) : base(db, "Classes")
        {
        }

        public async Task<List<Class>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM Classes WHERE TeacherId = @TeacherId ORDER BY Name";
            return await _db.QueryAsync<Class>(sql, new { TeacherId = teacherId });
        }

        public async Task<Class?> GetWithStudentsAsync(int id)
        {
            var @class = await GetByIdAsync(id);
            if (@class == null)
            {
                return null;
            }

            @class.Students = await _db.QueryAsync<Student>(
                "SELECT * FROM Students WHERE ClassId = @ClassId ORDER BY CreatedAt",
                new { ClassId = id });

            return @class;
        }

        public async Task<Class?> GetWithTeacherAsync(int id)
        {
            var @class = await GetByIdAsync(id);
            if (@class == null)
            {
                return null;
            }

            await LoadTeacherAsync(@class);
            return @class;
        }

        public async Task<List<Class>> GetAllWithTeacherAsync()
        {
            var classes = await GetAllAsync();
            await LoadTeachersAsync(classes);
            return classes;
        }

        public async Task<int> GetTestCountAsync(int classId)
        {
            var sql = @"
                SELECT
                    (SELECT COUNT(*) FROM RegularTestClasses WHERE ClassId = @ClassId) +
                    (SELECT COUNT(*) FROM SpellingTestClasses WHERE ClassId = @ClassId) +
                    (SELECT COUNT(*) FROM PunctuationTestClasses WHERE ClassId = @ClassId) +
                    (SELECT COUNT(*) FROM OrthoeopyTestClasses WHERE ClassId = @ClassId)";

            var count = await _db.QueryScalarAsync<int?>(sql, new { ClassId = classId });
            return count ?? 0;
        }

        public async Task<List<Student>> GetStudentsByClassIdsAsync(List<int> classIds)
        {
            if (classIds == null || classIds.Count == 0)
            {
                return new List<Student>();
            }

            var paramNames = new List<string>();
            var parameters = new ExpandoObject() as IDictionary<string, object>;

            for (var i = 0; i < classIds.Count; i++)
            {
                var paramName = $"Id{i}";
                paramNames.Add($"@{paramName}");
                parameters[paramName] = classIds[i];
            }

            var sql = $"SELECT * FROM Students WHERE ClassId IN ({string.Join(", ", paramNames)}) ORDER BY ClassId, CreatedAt";
            return await _db.QueryAsync<Student>(sql, parameters);
        }

        public override async Task<int> CreateAsync(Class entity)
        {
            var sql = @"
                INSERT INTO Classes (Name, Description, TeacherId, CreatedAt, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Description, @TeacherId, @CreatedAt, @IsActive)";

            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.Name,
                entity.Description,
                entity.TeacherId,
                entity.CreatedAt,
                entity.IsActive
            });

            return id;
        }

        public override async Task<int> UpdateAsync(Class entity)
        {
            var sql = @"
                UPDATE Classes
                SET Name = @Name,
                    Description = @Description,
                    IsActive = @IsActive
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Description,
                entity.IsActive
            });
        }

        private async Task LoadTeacherAsync(Class classItem)
        {
            var teacher = await _db.QueryFirstOrDefaultAsync<ApplicationUser>(
                "SELECT * FROM AspNetUsers WHERE Id = @Id",
                new { Id = classItem.TeacherId });

            if (teacher != null)
            {
                classItem.Teacher = teacher;
            }
        }

        private async Task LoadTeachersAsync(List<Class> classes)
        {
            var teacherIds = classes
                .Select(c => c.TeacherId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (!teacherIds.Any())
            {
                return;
            }

            var paramNames = new List<string>();
            var parameters = new ExpandoObject() as IDictionary<string, object>;

            for (var i = 0; i < teacherIds.Count; i++)
            {
                var paramName = $"TeacherId{i}";
                paramNames.Add($"@{paramName}");
                parameters[paramName] = teacherIds[i];
            }

            var sql = $"SELECT * FROM AspNetUsers WHERE Id IN ({string.Join(", ", paramNames)})";
            var teachers = await _db.QueryAsync<ApplicationUser>(sql, parameters);
            var teacherDictionary = teachers.ToDictionary(t => t.Id, t => t);

            foreach (var classItem in classes)
            {
                if (!string.IsNullOrEmpty(classItem.TeacherId) &&
                    teacherDictionary.TryGetValue(classItem.TeacherId, out var teacher))
                {
                    classItem.Teacher = teacher;
                }
            }
        }
    }
}

