using OnlineTutor2.Models;
using System.Text;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы со студентами
    /// </summary>
    public class StudentRepository : BaseRepository<Student>, IStudentRepository
    {
        public StudentRepository(IDatabaseConnection db) : base(db, "Students")
        {
        }

        public async Task<Student?> GetByUserIdAsync(string userId)
        {
            var sql = "SELECT * FROM Students WHERE UserId = @UserId";
            return await _db.QueryFirstOrDefaultAsync<Student>(sql, new { UserId = userId });
        }

        public async Task<List<Student>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM Students WHERE ClassId = @ClassId";
            return await _db.QueryAsync<Student>(sql, new { ClassId = classId });
        }

        public async Task<List<Student>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = @"
                SELECT s.* 
                FROM Students s
                INNER JOIN Classes c ON s.ClassId = c.Id
                WHERE c.TeacherId = @TeacherId";
            return await _db.QueryAsync<Student>(sql, new { TeacherId = teacherId });
        }

        public async Task<Student?> GetWithUserAsync(int id)
        {
            var sql = @"
                SELECT s.*, 
                       u.Id as User_Id, u.UserName as User_UserName, u.Email as User_Email,
                       u.FirstName as User_FirstName, u.LastName as User_LastName,
                       u.DateOfBirth as User_DateOfBirth, u.CreatedAt as User_CreatedAt,
                       u.LastLoginAt as User_LastLoginAt, u.IsActive as User_IsActive
                FROM Students s
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE s.Id = @Id";
            
            var result = await _db.QueryFirstOrDefaultAsync<Student>(sql, new { Id = id });
            // TODO: Маппинг навигационных свойств можно улучшить
            return result;
        }

        public async Task<List<Student>> GetAllWithUserAsync()
        {
            var sql = @"
                SELECT s.*, 
                       u.Id as User_Id, u.UserName as User_UserName, u.Email as User_Email,
                       u.FirstName as User_FirstName, u.LastName as User_LastName,
                       u.DateOfBirth as User_DateOfBirth, u.CreatedAt as User_CreatedAt,
                       u.LastLoginAt as User_LastLoginAt, u.IsActive as User_IsActive
                FROM Students s
                INNER JOIN AspNetUsers u ON s.UserId = u.Id";
            
            return await _db.QueryAsync<Student>(sql);
        }

        public override async Task<int> CreateAsync(Student entity)
        {
            var sql = @"
                INSERT INTO Students (UserId, ClassId, StudentNumber, School, Grade, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@UserId, @ClassId, @StudentNumber, @School, @Grade, @CreatedAt)";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(Student entity)
        {
            var sql = @"
                UPDATE Students 
                SET ClassId = @ClassId, StudentNumber = @StudentNumber, School = @School, Grade = @Grade
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<(List<Student> Students, int TotalCount)> GetPaginatedAsync(
            List<int>? teacherClassIds = null,
            string? searchString = null,
            int? classFilter = null,
            string? sortOrder = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var whereConditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Фильтрация: только студенты из классов текущего учителя или без класса
            if (teacherClassIds != null && teacherClassIds.Count > 0)
            {
                var classIdsString = string.Join(",", teacherClassIds);
                whereConditions.Add($"(s.ClassId IS NULL OR s.ClassId IN ({classIdsString}))");
            }
            else
            {
                whereConditions.Add("s.ClassId IS NULL");
            }

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                whereConditions.Add(@"(LOWER(u.FirstName) LIKE @Search OR 
                    LOWER(u.LastName) LIKE @Search OR 
                    LOWER(u.Email) LIKE @Search OR 
                    LOWER(s.School) LIKE @Search)");
                parameters["Search"] = $"%{searchString.ToLower()}%";
            }

            // Фильтрация по классу
            if (classFilter.HasValue)
            {
                if (classFilter.Value == 0)
                {
                    whereConditions.Add("s.ClassId IS NULL");
                }
                else
                {
                    whereConditions.Add("s.ClassId = @ClassFilter");
                    parameters["ClassFilter"] = classFilter.Value;
                }
            }

            // Определяем сортировку
            var orderBy = "u.LastName ASC";
            switch (sortOrder)
            {
                case "name_desc":
                    orderBy = "u.LastName DESC";
                    break;
                case "Date":
                    orderBy = "s.CreatedAt ASC";
                    break;
                case "date_desc":
                    orderBy = "s.CreatedAt DESC";
                    break;
                default:
                    orderBy = "u.LastName ASC";
                    break;
            }

            // Получаем общее количество
            var countSql = $@"
                SELECT COUNT(*)
                FROM Students s
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE {string.Join(" AND ", whereConditions)}";
            var totalCount = await _db.QueryScalarAsync<int>(countSql, parameters);

            // Пагинация
            var offset = (pageNumber - 1) * pageSize;
            var studentsSql = $@"
                SELECT s.*, u.*
                FROM Students s
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE {string.Join(" AND ", whereConditions)}
                ORDER BY {orderBy}
                OFFSET {offset} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";

            var students = await _db.QueryAsync<Student>(studentsSql, parameters);
            return (students, totalCount);
        }

        public async Task<List<string>> GetUserIdsInClassesAsync(List<int> classIds)
        {
            if (classIds == null || classIds.Count == 0)
                return new List<string>();

            var classIdsString = string.Join(",", classIds);
            var sql = $"SELECT DISTINCT UserId FROM Students WHERE ClassId IN ({classIdsString})";
            var userIds = await _db.QueryAsync<string>(sql);
            return userIds;
        }

        public async Task<List<ApplicationUser>> GetAvailableStudentsAsync(
            HashSet<string> excludedUserIds,
            string? searchString = null)
        {
            var whereConditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (excludedUserIds != null && excludedUserIds.Count > 0)
            {
                var userIdsString = string.Join("','", excludedUserIds);
                whereConditions.Add($"u.Id NOT IN ('{userIdsString}')");
            }

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                whereConditions.Add(@"(LOWER(u.FirstName) LIKE @Search OR 
                    LOWER(u.LastName) LIKE @Search OR 
                    LOWER(u.Email) LIKE @Search OR 
                    LOWER(s.School) LIKE @Search)");
                parameters["Search"] = $"%{searchString.ToLower()}%";
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            var sql = $@"
                SELECT u.*
                FROM AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                LEFT JOIN Students s ON u.Id = s.UserId
                {whereClause}
                AND r.Name = 'Student'
                ORDER BY u.LastName";

            return await _db.QueryAsync<ApplicationUser>(sql, parameters);
        }

        public async Task<string?> GetLastStudentNumberAsync(int year)
        {
            var sql = $@"
                SELECT TOP 1 StudentNumber
                FROM Students
                WHERE StudentNumber IS NOT NULL 
                    AND StudentNumber LIKE '{year}%'
                ORDER BY StudentNumber DESC";

            return await _db.QueryScalarAsync<string>(sql);
        }

        public async Task<int> GetDistinctStudentCountByTeacherIdAsync(string teacherId)
        {
            var sql = @"
                SELECT COUNT(DISTINCT s.Id)
                FROM Students s
                INNER JOIN Classes c ON s.ClassId = c.Id
                WHERE c.TeacherId = @TeacherId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId });
            return result ?? 0;
        }
    }
}

