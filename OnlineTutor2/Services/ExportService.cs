using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using System.Text;

namespace OnlineTutor2.Services
{
    public class ExportService : IExportService
    {
        private readonly IDatabaseConnection _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStudentRepository _studentRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IClassRepository _classRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly ILogger<ExportService> _logger;

        public ExportService(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            IStudentRepository studentRepository,
            ITeacherRepository teacherRepository,
            IClassRepository classRepository,
            IRegularTestResultRepository regularTestResultRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            ILogger<ExportService> logger)
        {
            _db = db;
            _userManager = userManager;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
            _classRepository = classRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _logger = logger;
        }

        #region Users Export

        public async Task<byte[]> ExportUsersToExcelAsync()
        {
            var usersSql = "SELECT * FROM AspNetUsers ORDER BY LastName";
            var users = await _db.QueryAsync<ApplicationUser>(usersSql);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Пользователи");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Фамилия";
            worksheet.Cells[1, 3].Value = "Имя";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Телефон";
            worksheet.Cells[1, 6].Value = "Дата рождения";
            worksheet.Cells[1, 7].Value = "Возраст";
            worksheet.Cells[1, 8].Value = "Роли";
            worksheet.Cells[1, 9].Value = "Статус";
            worksheet.Cells[1, 10].Value = "Дата регистрации";

            // Стиль заголовков
            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 2;
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                worksheet.Cells[row, 1].Value = user.Id;
                worksheet.Cells[row, 2].Value = user.LastName;
                worksheet.Cells[row, 3].Value = user.FirstName;
                worksheet.Cells[row, 4].Value = user.Email;
                worksheet.Cells[row, 5].Value = user.PhoneNumber ?? "-";
                worksheet.Cells[row, 6].Value = user.DateOfBirth.ToString("dd.MM.yyyy");
                worksheet.Cells[row, 7].Value = user.Age;
                worksheet.Cells[row, 8].Value = string.Join(", ", roles);
                worksheet.Cells[row, 9].Value = user.IsActive ? "Активен" : "Заблокирован";
                worksheet.Cells[row, 10].Value = user.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                row++;
            }

            // Автоширина колонок
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportUsersToCSVAsync()
        {
            var usersSql = "SELECT * FROM AspNetUsers ORDER BY LastName";
            var users = await _db.QueryAsync<ApplicationUser>(usersSql);

            var csv = new StringBuilder();
            csv.AppendLine("ID;Фамилия;Имя;Email;Телефон;Дата рождения;Возраст;Роли;Статус;Дата регистрации");

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                csv.AppendLine($"{user.Id};{user.LastName};{user.FirstName};{user.Email};{user.PhoneNumber ?? "-"};" +
                    $"{user.DateOfBirth:dd.MM.yyyy};{user.Age};{string.Join(", ", roles)};" +
                    $"{(user.IsActive ? "Активен" : "Заблокирован")};{user.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Teachers Export

        public async Task<byte[]> ExportTeachersToExcelAsync()
        {
            var teachers = await _teacherRepository.GetAllWithUserAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Учителя");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "ФИО";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Предмет";
            worksheet.Cells[1, 5].Value = "Образование";
            worksheet.Cells[1, 6].Value = "Опыт (лет)";
            worksheet.Cells[1, 7].Value = "Классов";
            worksheet.Cells[1, 8].Value = "Тестов";
            worksheet.Cells[1, 9].Value = "Статус модерации";
            worksheet.Cells[1, 10].Value = "Статус аккаунта";

            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var teacher in teachers)
            {
                var user = await _userManager.FindByIdAsync(teacher.UserId);
                var classesCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM Classes WHERE TeacherId = @TeacherId", new { TeacherId = teacher.UserId });
                var testsCount = await _db.QueryScalarAsync<int>(
                    "(SELECT COUNT(*) FROM RegularTests WHERE TeacherId = @TeacherId) + " +
                    "(SELECT COUNT(*) FROM SpellingTests WHERE TeacherId = @TeacherId) + " +
                    "(SELECT COUNT(*) FROM PunctuationTests WHERE TeacherId = @TeacherId) + " +
                    "(SELECT COUNT(*) FROM OrthoeopyTests WHERE TeacherId = @TeacherId)",
                    new { TeacherId = teacher.UserId });

                worksheet.Cells[row, 1].Value = teacher.Id;
                worksheet.Cells[row, 2].Value = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";
                worksheet.Cells[row, 3].Value = user?.Email ?? "-";
                worksheet.Cells[row, 4].Value = teacher.Subject ?? "-";
                worksheet.Cells[row, 5].Value = teacher.Education ?? "-";
                worksheet.Cells[row, 6].Value = teacher.Experience?.ToString() ?? "-";
                worksheet.Cells[row, 7].Value = classesCount;
                worksheet.Cells[row, 8].Value = testsCount;
                worksheet.Cells[row, 9].Value = teacher.IsApproved ? "Одобрен" : "На модерации";
                worksheet.Cells[row, 10].Value = user?.IsActive == true ? "Активен" : "Заблокирован";

                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportTeachersToCSVAsync()
        {
            var teachers = await _teacherRepository.GetAllWithUserAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;ФИО;Email;Предмет;Образование;Опыт;Классов;Тестов;Модерация;Статус");

            foreach (var teacher in teachers)
            {
                var user = await _userManager.FindByIdAsync(teacher.UserId);
                var classesCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM Classes WHERE TeacherId = @TeacherId", new { TeacherId = teacher.UserId });
                var testsCount = await _db.QueryScalarAsync<int>(
                    "(SELECT COUNT(*) FROM RegularTests WHERE TeacherId = @TeacherId) + " +
                    "(SELECT COUNT(*) FROM SpellingTests WHERE TeacherId = @TeacherId) + " +
                    "(SELECT COUNT(*) FROM PunctuationTests WHERE TeacherId = @TeacherId) + " +
                    "(SELECT COUNT(*) FROM OrthoeopyTests WHERE TeacherId = @TeacherId)",
                    new { TeacherId = teacher.UserId });

                csv.AppendLine($"{teacher.Id};{(user != null ? $"{user.FirstName} {user.LastName}" : "Unknown")};{user?.Email ?? "-"};" +
                    $"{teacher.Subject ?? "-"};{teacher.Education ?? "-"};{teacher.Experience?.ToString() ?? "-"};" +
                    $"{classesCount};{testsCount};" +
                    $"{(teacher.IsApproved ? "Одобрен" : "На модерации")};" +
                    $"{(user?.IsActive == true ? "Активен" : "Заблокирован")}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Students Export

        public async Task<byte[]> ExportStudentsToExcelAsync()
        {
            var students = await _studentRepository.GetAllWithUserAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Студенты");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "ФИО";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Школа";
            worksheet.Cells[1, 5].Value = "Класс в школе";
            worksheet.Cells[1, 6].Value = "Класс на платформе";
            worksheet.Cells[1, 7].Value = "Тестов пройдено";
            worksheet.Cells[1, 8].Value = "Средний балл";
            worksheet.Cells[1, 9].Value = "Статус";

            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCyan);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var student in students)
            {
                var user = await _userManager.FindByIdAsync(student.UserId);
                var @class = student.ClassId.HasValue ? await _classRepository.GetByIdAsync(student.ClassId.Value) : null;
                
                // Получаем статистику через SQL
                var completedTestsSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM RegularTestResults WHERE StudentId = @StudentId AND IsCompleted = 1) +
                        (SELECT COUNT(*) FROM SpellingTestResults WHERE StudentId = @StudentId AND IsCompleted = 1) +
                        (SELECT COUNT(*) FROM PunctuationTestResults WHERE StudentId = @StudentId AND IsCompleted = 1) +
                        (SELECT COUNT(*) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND IsCompleted = 1)";
                var completedTests = await _db.QueryScalarAsync<int>(completedTestsSql, new { StudentId = student.Id });

                // Получаем средний балл через SQL
                var avgScoreSql = @"
                    SELECT AVG(CAST(Percentage AS FLOAT))
                    FROM (
                        SELECT Percentage FROM RegularTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                        UNION ALL
                        SELECT Percentage FROM SpellingTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                        UNION ALL
                        SELECT Percentage FROM PunctuationTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                        UNION ALL
                        SELECT Percentage FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                    ) AS AllResults";
                var avgScore = await _db.QueryScalarAsync<double?>(avgScoreSql, new { StudentId = student.Id }) ?? 0;

                worksheet.Cells[row, 1].Value = student.Id;
                worksheet.Cells[row, 2].Value = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";
                worksheet.Cells[row, 3].Value = user?.Email ?? "-";
                worksheet.Cells[row, 4].Value = student.School ?? "-";
                worksheet.Cells[row, 5].Value = student.Grade?.ToString() ?? "-";
                worksheet.Cells[row, 6].Value = @class?.Name ?? "-";
                worksheet.Cells[row, 7].Value = completedTests;
                worksheet.Cells[row, 8].Value = $"{avgScore:F1}%";
                worksheet.Cells[row, 9].Value = user?.IsActive == true ? "Активен" : "Заблокирован";

                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportStudentsToCSVAsync()
        {
            var students = await _studentRepository.GetAllWithUserAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;ФИО;Email;Школа;Класс в школе;Класс на платформе;Тестов пройдено;Средний балл;Статус");

            foreach (var student in students)
            {
                var user = await _userManager.FindByIdAsync(student.UserId);
                var @class = student.ClassId.HasValue ? await _classRepository.GetByIdAsync(student.ClassId.Value) : null;
                
                // Получаем статистику через SQL
                var completedTestsSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM RegularTestResults WHERE StudentId = @StudentId AND IsCompleted = 1) +
                        (SELECT COUNT(*) FROM SpellingTestResults WHERE StudentId = @StudentId AND IsCompleted = 1) +
                        (SELECT COUNT(*) FROM PunctuationTestResults WHERE StudentId = @StudentId AND IsCompleted = 1) +
                        (SELECT COUNT(*) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND IsCompleted = 1)";
                var completedTests = await _db.QueryScalarAsync<int>(completedTestsSql, new { StudentId = student.Id });

                // Получаем средний балл через SQL
                var avgScoreSql = @"
                    SELECT AVG(CAST(Percentage AS FLOAT))
                    FROM (
                        SELECT Percentage FROM RegularTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                        UNION ALL
                        SELECT Percentage FROM SpellingTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                        UNION ALL
                        SELECT Percentage FROM PunctuationTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                        UNION ALL
                        SELECT Percentage FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND IsCompleted = 1
                    ) AS AllResults";
                var avgScore = await _db.QueryScalarAsync<double?>(avgScoreSql, new { StudentId = student.Id }) ?? 0;

                csv.AppendLine($"{student.Id};{(user != null ? $"{user.FirstName} {user.LastName}" : "Unknown")};{user?.Email ?? "-"};" +
                    $"{student.School ?? "-"};{student.Grade?.ToString() ?? "-"};{@class?.Name ?? "-"};" +
                    $"{completedTests};{avgScore:F1}%;{(user?.IsActive == true ? "Активен" : "Заблокирован")}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Classes Export

        public async Task<byte[]> ExportClassesToExcelAsync()
        {
            var classes = await _classRepository.GetAllAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Классы");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Название";
            worksheet.Cells[1, 3].Value = "Описание";
            worksheet.Cells[1, 4].Value = "Учитель";
            worksheet.Cells[1, 5].Value = "Студентов";
            worksheet.Cells[1, 6].Value = "Орфография";
            worksheet.Cells[1, 7].Value = "Классические";
            worksheet.Cells[1, 8].Value = "Пунктуация";
            worksheet.Cells[1, 9].Value = "Орфоэпия";
            worksheet.Cells[1, 10].Value = "Всего тестов";
            worksheet.Cells[1, 11].Value = "Дата создания";

            using (var range = worksheet.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var classItem in classes)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(classItem.TeacherId);
                var teacherUser = teacher != null ? await _userManager.FindByIdAsync(classItem.TeacherId) : null;
                var students = await _studentRepository.GetByClassIdAsync(classItem.Id);
                
                var regularTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM RegularTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var spellingTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM SpellingTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var punctuationTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM PunctuationTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var orthoeopyTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM OrthoeopyTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var totalTests = regularTestsCount + spellingTestsCount + punctuationTestsCount + orthoeopyTestsCount;

                worksheet.Cells[row, 1].Value = classItem.Id;
                worksheet.Cells[row, 2].Value = classItem.Name;
                worksheet.Cells[row, 3].Value = classItem.Description ?? "-";
                worksheet.Cells[row, 4].Value = teacherUser != null ? $"{teacherUser.FirstName} {teacherUser.LastName}" : "Unknown";
                worksheet.Cells[row, 5].Value = students.Count;
                worksheet.Cells[row, 6].Value = spellingTestsCount;
                worksheet.Cells[row, 7].Value = regularTestsCount;
                worksheet.Cells[row, 8].Value = punctuationTestsCount;
                worksheet.Cells[row, 9].Value = orthoeopyTestsCount;
                worksheet.Cells[row, 10].Value = totalTests;
                worksheet.Cells[row, 11].Value = classItem.CreatedAt.ToString("dd.MM.yyyy");

                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportClassesToCSVAsync()
        {
            var classes = await _classRepository.GetAllAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;Название;Описание;Учитель;Студентов;Орфография;Классические;Пунктуация;Орфоэпия;Всего тестов;Дата создания");

            foreach (var classItem in classes)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(classItem.TeacherId);
                var teacherUser = teacher != null ? await _userManager.FindByIdAsync(classItem.TeacherId) : null;
                var students = await _studentRepository.GetByClassIdAsync(classItem.Id);
                
                var regularTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM RegularTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var spellingTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM SpellingTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var punctuationTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM PunctuationTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var orthoeopyTestsCount = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM OrthoeopyTestClasses WHERE ClassId = @ClassId", new { ClassId = classItem.Id });
                var totalTests = regularTestsCount + spellingTestsCount + punctuationTestsCount + orthoeopyTestsCount;

                csv.AppendLine($"{classItem.Id};{classItem.Name};{classItem.Description ?? "-"};" +
                    $"{(teacherUser != null ? $"{teacherUser.FirstName} {teacherUser.LastName}" : "Unknown")};{students.Count};" +
                    $"{spellingTestsCount};{regularTestsCount};" +
                    $"{punctuationTestsCount};{orthoeopyTestsCount};" +
                    $"{totalTests};{classItem.CreatedAt:dd.MM.yyyy}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Tests Export

        public async Task<byte[]> ExportTestsToExcelAsync()
        {
            var tests = await TestExportHelper.GetAllTestsForExportAsync(_db);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Тесты");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Название";
            worksheet.Cells[1, 3].Value = "Тип";
            worksheet.Cells[1, 4].Value = "Учитель";
            worksheet.Cells[1, 5].Value = "Классы";
            worksheet.Cells[1, 6].Value = "Вопросов";
            worksheet.Cells[1, 7].Value = "Результатов";
            worksheet.Cells[1, 8].Value = "Статус";
            worksheet.Cells[1, 9].Value = "Дата создания";

            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGoldenrodYellow);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 2;
            foreach (var test in tests)
            {
                worksheet.Cells[row, 1].Value = test.Id;
                worksheet.Cells[row, 2].Value = test.Title;
                worksheet.Cells[row, 3].Value = test.Type;
                worksheet.Cells[row, 4].Value = test.TeacherName;
                worksheet.Cells[row, 5].Value = test.ClassNames;
                worksheet.Cells[row, 6].Value = test.QuestionsCount;
                worksheet.Cells[row, 7].Value = test.ResultsCount;
                worksheet.Cells[row, 8].Value = test.IsActive ? "Активен" : "Неактивен";
                worksheet.Cells[row, 9].Value = test.CreatedAt.ToString("dd.MM.yyyy");
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportTestsToCSVAsync()
        {
            var tests = await TestExportHelper.GetAllTestsForExportAsync(_db);

            var csv = new StringBuilder();
            csv.AppendLine("ID;Название;Тип;Учитель;Классы;Вопросов;Результатов;Статус;Дата создания");

            foreach (var test in tests)
            {
                csv.AppendLine($"{test.Id};{test.Title};{test.Type};{test.TeacherName};" +
                    $"{test.ClassNames};{test.QuestionsCount};" +
                    $"{test.ResultsCount};{(test.IsActive ? "Активен" : "Неактивен")};" +
                    $"{test.CreatedAt:dd.MM.yyyy}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Test Results Export

        public async Task<byte[]> ExportTestResultsToExcelAsync()
        {
            var results = await TestExportHelper.GetAllTestResultsForExportAsync(_db);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Результаты тестов");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Тест";
            worksheet.Cells[1, 3].Value = "Тип";
            worksheet.Cells[1, 4].Value = "Студент";
            worksheet.Cells[1, 5].Value = "Баллов";
            worksheet.Cells[1, 6].Value = "Макс. баллов";
            worksheet.Cells[1, 7].Value = "Процент";
            worksheet.Cells[1, 8].Value = "Начало";
            worksheet.Cells[1, 9].Value = "Завершение";
            worksheet.Cells[1, 10].Value = "Статус";

            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSalmon);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Данные
            int row = 2;
            foreach (var result in results)
            {
                worksheet.Cells[row, 1].Value = result.Id;
                worksheet.Cells[row, 2].Value = result.TestTitle;
                worksheet.Cells[row, 3].Value = result.TestType;
                worksheet.Cells[row, 4].Value = result.StudentName;
                worksheet.Cells[row, 5].Value = result.Score;
                worksheet.Cells[row, 6].Value = result.MaxScore;
                worksheet.Cells[row, 7].Value = $"{result.Percentage:F1}%";
                worksheet.Cells[row, 8].Value = result.StartedAt.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 9].Value = result.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
                worksheet.Cells[row, 10].Value = result.IsCompleted ? "Завершен" : "В процессе";
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportTestResultsToCSVAsync()
        {
            var results = await TestExportHelper.GetAllTestResultsForExportAsync(_db);

            var csv = new StringBuilder();
            csv.AppendLine("ID;Тест;Тип;Студент;Баллов;Макс. баллов;Процент;Начало;Завершение;Статус");

            foreach (var result in results)
            {
                csv.AppendLine($"{result.Id};{result.TestTitle};{result.TestType};" +
                    $"{result.StudentName};{result.Score};{result.MaxScore};" +
                    $"{result.Percentage:F1}%;{result.StartedAt:dd.MM.yyyy HH:mm};" +
                    $"{result.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"};" +
                    $"{(result.IsCompleted ? "Завершен" : "В процессе")}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Audit Logs Export

        public async Task<byte[]> ExportAuditLogsToExcelAsync()
        {
            var logsSql = @"
                SELECT TOP 10000 al.*, 
                       u.Id as User_Id, u.Email as User_Email, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM AuditLogs al
                LEFT JOIN AspNetUsers u ON al.UserId = u.Id
                ORDER BY al.CreatedAt DESC";
            var logs = await _db.QueryAsync<AuditLog>(logsSql);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Журнал действий");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Дата и время";
            worksheet.Cells[1, 3].Value = "Администратор";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Действие";
            worksheet.Cells[1, 6].Value = "Тип сущности";
            worksheet.Cells[1, 7].Value = "ID сущности";
            worksheet.Cells[1, 8].Value = "Детали";
            worksheet.Cells[1, 9].Value = "IP адрес";

            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var log in logs)
            {
                var user = await _userManager.FindByIdAsync(log.UserId ?? "");
                worksheet.Cells[row, 1].Value = log.Id;
                worksheet.Cells[row, 2].Value = log.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
                worksheet.Cells[row, 3].Value = log.UserName;
                worksheet.Cells[row, 4].Value = user?.Email ?? "-";
                worksheet.Cells[row, 5].Value = log.Action;
                worksheet.Cells[row, 6].Value = log.EntityType;
                worksheet.Cells[row, 7].Value = log.EntityId ?? "-";
                worksheet.Cells[row, 8].Value = log.Details ?? "-";
                worksheet.Cells[row, 9].Value = log.IpAddress ?? "-";
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportAuditLogsToCSVAsync()
        {
            var logsSql = @"
                SELECT TOP 10000 *
                FROM AuditLogs
                ORDER BY CreatedAt DESC";
            var logs = await _db.QueryAsync<AuditLog>(logsSql);

            var csv = new StringBuilder();
            csv.AppendLine("ID;Дата и время;Администратор;Email;Действие;Тип сущности;ID сущности;Детали;IP адрес");

            foreach (var log in logs)
            {
                var user = await _userManager.FindByIdAsync(log.UserId ?? "");
                csv.AppendLine($"{log.Id};{log.CreatedAt:dd.MM.yyyy HH:mm:ss};{log.UserName};" +
                    $"{user?.Email ?? "-"};{log.Action};{log.EntityType};" +
                    $"{log.EntityId ?? "-"};{log.Details ?? "-"};{log.IpAddress ?? "-"}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Full System Export

        public async Task<byte[]> ExportFullSystemToExcelAsync()
        {
            using var package = new ExcelPackage();

            // Добавляем все данные в разные листы
            _logger.LogInformation("Exporting all system data to Excel...");

            // Пользователи
            var usersData = await ExportUsersToExcelAsync();
            var usersPackage = new ExcelPackage(new MemoryStream(usersData));
            var usersWorksheet = usersPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Пользователи", usersWorksheet);

            // Учителя
            var teachersData = await ExportTeachersToExcelAsync();
            var teachersPackage = new ExcelPackage(new MemoryStream(teachersData));
            var teachersWorksheet = teachersPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Учителя", teachersWorksheet);

            // Студенты
            var studentsData = await ExportStudentsToExcelAsync();
            var studentsPackage = new ExcelPackage(new MemoryStream(studentsData));
            var studentsWorksheet = studentsPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Студенты", studentsWorksheet);

            // Классы
            var classesData = await ExportClassesToExcelAsync();
            var classesPackage = new ExcelPackage(new MemoryStream(classesData));
            var classesWorksheet = classesPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Классы", classesWorksheet);

            // Тесты
            var testsData = await ExportTestsToExcelAsync();
            var testsPackage = new ExcelPackage(new MemoryStream(testsData));
            var testsWorksheet = testsPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Тесты", testsWorksheet);

            // Результаты
            var resultsData = await ExportTestResultsToExcelAsync();
            var resultsPackage = new ExcelPackage(new MemoryStream(resultsData));
            var resultsWorksheet = resultsPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Результаты", resultsWorksheet);

            // Журнал
            var logsData = await ExportAuditLogsToExcelAsync();
            var logsPackage = new ExcelPackage(new MemoryStream(logsData));
            var logsWorksheet = logsPackage.Workbook.Worksheets[0];
            package.Workbook.Worksheets.Add("Журнал действий", logsWorksheet);

            _logger.LogInformation("Full system export completed");

            return package.GetAsByteArray();
        }

        #endregion
    }
}
