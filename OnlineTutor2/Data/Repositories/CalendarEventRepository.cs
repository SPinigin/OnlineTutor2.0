using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class CalendarEventRepository : BaseRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(IDatabaseConnection db) : base(db, "CalendarEvents")
        {
        }

        public async Task<List<CalendarEvent>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM CalendarEvents WHERE TeacherId = @TeacherId ORDER BY StartDateTime";
            return await _db.QueryAsync<CalendarEvent>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<CalendarEvent>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM CalendarEvents WHERE ClassId = @ClassId ORDER BY StartDateTime";
            return await _db.QueryAsync<CalendarEvent>(sql, new { ClassId = classId });
        }

        public async Task<List<CalendarEvent>> GetByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM CalendarEvents WHERE StudentId = @StudentId ORDER BY StartDateTime";
            return await _db.QueryAsync<CalendarEvent>(sql, new { StudentId = studentId });
        }

        public async Task<List<CalendarEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var sql = @"
                SELECT * FROM CalendarEvents 
                WHERE StartDateTime >= @StartDate AND EndDateTime <= @EndDate 
                ORDER BY StartDateTime";
            return await _db.QueryAsync<CalendarEvent>(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public override async Task<int> CreateAsync(CalendarEvent entity)
        {
            var sql = @"
                INSERT INTO CalendarEvents (
                    Title, Description, StartDateTime, EndDateTime, TeacherId, ClassId, StudentId,
                    Location, Color, IsRecurring, RecurrencePattern, IsCompleted, Notes, CreatedAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @Description, @StartDateTime, @EndDateTime, @TeacherId, @ClassId, @StudentId,
                    @Location, @Color, @IsRecurring, @RecurrencePattern, @IsCompleted, @Notes, @CreatedAt
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(CalendarEvent entity)
        {
            var sql = @"
                UPDATE CalendarEvents 
                SET Title = @Title, Description = @Description, StartDateTime = @StartDateTime,
                    EndDateTime = @EndDateTime, Location = @Location, Color = @Color,
                    IsRecurring = @IsRecurring, RecurrencePattern = @RecurrencePattern,
                    IsCompleted = @IsCompleted, Notes = @Notes, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        public async Task<int> GetUpcomingCountAsync(string teacherId, DateTime now)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM CalendarEvents 
                WHERE TeacherId = @TeacherId 
                    AND StartDateTime >= @Now 
                    AND IsCompleted = 0";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId, Now = now });
            return result ?? 0;
        }

        public async Task<int> GetTodayCountAsync(string teacherId, DateTime now)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM CalendarEvents 
                WHERE TeacherId = @TeacherId 
                    AND CAST(StartDateTime AS DATE) = CAST(@Now AS DATE)";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId, Now = now });
            return result ?? 0;
        }

        public async Task<int> GetCompletedThisMonthCountAsync(string teacherId, DateTime now)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM CalendarEvents 
                WHERE TeacherId = @TeacherId 
                    AND IsCompleted = 1
                    AND MONTH(StartDateTime) = MONTH(@Now)
                    AND YEAR(StartDateTime) = YEAR(@Now)";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId, Now = now });
            return result ?? 0;
        }

        public async Task<List<CalendarEvent>> GetByTeacherIdInDateRangeAsync(string teacherId, DateTime? start, DateTime? end)
        {
            var searchStart = start ?? DateTime.Now.AddMonths(-3);
            var searchEnd = end ?? DateTime.Now.AddMonths(6);
            
            var sql = @"
                SELECT * 
                FROM CalendarEvents 
                WHERE TeacherId = @TeacherId 
                    AND (EndDateTime >= @SearchStart OR IsRecurring = 1)
                    AND StartDateTime <= @SearchEnd
                ORDER BY StartDateTime";
            return await _db.QueryAsync<CalendarEvent>(sql, new { TeacherId = teacherId, SearchStart = searchStart, SearchEnd = searchEnd });
        }

        public async Task<CalendarEvent?> GetByIdWithRelationsAsync(int id, string teacherId)
        {
            var sql = @"
                SELECT * 
                FROM CalendarEvents 
                WHERE Id = @Id AND TeacherId = @TeacherId";
            return await _db.QueryFirstOrDefaultAsync<CalendarEvent>(sql, new { Id = id, TeacherId = teacherId });
        }
    }
}

