using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class MaterialRepository : BaseRepository<Material>, IMaterialRepository
    {
        public MaterialRepository(IDatabaseConnection db) : base(db, "Materials")
        {
        }

        public async Task<List<Material>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM Materials WHERE ClassId = @ClassId ORDER BY UploadedAt DESC";
            return await _db.QueryAsync<Material>(sql, new { ClassId = classId });
        }

        public async Task<List<Material>> GetByUploadedByIdAsync(string uploadedById)
        {
            var sql = "SELECT * FROM Materials WHERE UploadedById = @UploadedById ORDER BY UploadedAt DESC";
            return await _db.QueryAsync<Material>(sql, new { UploadedById = uploadedById });
        }

        public async Task<List<Material>> GetActiveByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM Materials WHERE ClassId = @ClassId AND IsActive = 1 ORDER BY UploadedAt DESC";
            return await _db.QueryAsync<Material>(sql, new { ClassId = classId });
        }

        public override async Task<int> CreateAsync(Material entity)
        {
            var sql = @"
                INSERT INTO Materials (
                    Title, Description, FilePath, FileName, FileSize, ContentType, Type,
                    ClassId, UploadedById, UploadedAt, IsActive
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @Description, @FilePath, @FileName, @FileSize, @ContentType, @Type,
                    @ClassId, @UploadedById, @UploadedAt, @IsActive
                )";
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.Title,
                entity.Description,
                entity.FilePath,
                entity.FileName,
                entity.FileSize,
                entity.ContentType,
                Type = (int)entity.Type,
                entity.ClassId,
                entity.UploadedById,
                entity.UploadedAt,
                entity.IsActive
            });
            return id;
        }

        public override async Task<int> UpdateAsync(Material entity)
        {
            var sql = @"
                UPDATE Materials 
                SET Title = @Title, Description = @Description, IsActive = @IsActive
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Title,
                entity.Description,
                entity.IsActive
            });
        }

        public async Task<List<Material>> GetFilteredAsync(
            string? uploadedById,
            string? searchString = null,
            int? classFilter = null,
            MaterialType? typeFilter = null,
            string? sortOrder = null)
        {
            var conditions = new List<string>();
            var parameters = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            
            conditions.Add("UploadedById = @UploadedById");
            parameters["UploadedById"] = uploadedById;

            if (!string.IsNullOrEmpty(searchString))
            {
                conditions.Add("(Title LIKE @SearchString OR Description LIKE @SearchString OR FileName LIKE @SearchString)");
                parameters["SearchString"] = $"%{searchString}%";
            }

            if (classFilter.HasValue)
            {
                conditions.Add("ClassId = @ClassId");
                parameters["ClassId"] = classFilter.Value;
            }

            if (typeFilter.HasValue)
            {
                conditions.Add("Type = @Type");
                parameters["Type"] = (int)typeFilter.Value;
            }

            var whereClause = "WHERE " + string.Join(" AND ", conditions);
            
            var orderBy = sortOrder switch
            {
                "title_desc" => "ORDER BY Title DESC",
                "Date" => "ORDER BY UploadedAt ASC",
                "date_desc" => "ORDER BY UploadedAt DESC",
                "Size" => "ORDER BY FileSize ASC",
                "size_desc" => "ORDER BY FileSize DESC",
                _ => "ORDER BY Title ASC"
            };

            var sql = $"SELECT * FROM Materials {whereClause} {orderBy}";
            return await _db.QueryAsync<Material>(sql, parameters);
        }

        public async Task<Material?> GetByIdWithClassAsync(int id, string uploadedById)
        {
            var sql = @"
                SELECT m.*
                FROM Materials m
                WHERE m.Id = @Id AND m.UploadedById = @UploadedById";
            return await _db.QueryFirstOrDefaultAsync<Material>(sql, new { Id = id, UploadedById = uploadedById });
        }
    }
}

