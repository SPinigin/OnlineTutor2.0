namespace OnlineTutor2.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportUsersToExcelAsync();
        Task<byte[]> ExportUsersToCSVAsync();

        Task<byte[]> ExportTeachersToExcelAsync();
        Task<byte[]> ExportTeachersToCSVAsync();

        Task<byte[]> ExportStudentsToExcelAsync();
        Task<byte[]> ExportStudentsToCSVAsync();

        Task<byte[]> ExportClassesToExcelAsync();
        Task<byte[]> ExportClassesToCSVAsync();

        Task<byte[]> ExportTestsToExcelAsync();
        Task<byte[]> ExportTestsToCSVAsync();

        Task<byte[]> ExportTestResultsToExcelAsync();
        Task<byte[]> ExportTestResultsToCSVAsync();

        Task<byte[]> ExportAuditLogsToExcelAsync();
        Task<byte[]> ExportAuditLogsToCSVAsync();

        Task<byte[]> ExportFullSystemToExcelAsync();
    }
}
