using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ICalendarEventRepository : IRepository<CalendarEvent>
    {
        Task<List<CalendarEvent>> GetByTeacherIdAsync(string teacherId);
        Task<List<CalendarEvent>> GetByClassIdAsync(int classId);
        Task<List<CalendarEvent>> GetByStudentIdAsync(int studentId);
        Task<List<CalendarEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetUpcomingCountAsync(string teacherId, DateTime now);
        Task<int> GetTodayCountAsync(string teacherId, DateTime now);
        Task<int> GetCompletedThisMonthCountAsync(string teacherId, DateTime now);
        Task<List<CalendarEvent>> GetByTeacherIdInDateRangeAsync(string teacherId, DateTime? start, DateTime? end);
        Task<CalendarEvent?> GetByIdWithRelationsAsync(int id, string teacherId);
    }
}

