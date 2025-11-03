using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using System.Text;

namespace OnlineTutor2.Services
{
    public class ExportService : IExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExportService> _logger;

        public ExportService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ExportService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        #region Users Export

        public async Task<byte[]> ExportUsersToExcelAsync()
        {
            var users = await _userManager.Users
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .OrderBy(u => u.LastName)
                .ToListAsync();

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
            var users = await _userManager.Users
                .OrderBy(u => u.LastName)
                .ToListAsync();

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
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.User.TeacherClasses)
                .Include(t => t.User.CreatedTests)
                .OrderBy(t => t.User.LastName)
                .ToListAsync();

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
                worksheet.Cells[row, 1].Value = teacher.Id;
                worksheet.Cells[row, 2].Value = teacher.User.FullName;
                worksheet.Cells[row, 3].Value = teacher.User.Email;
                worksheet.Cells[row, 4].Value = teacher.Subject ?? "-";
                worksheet.Cells[row, 5].Value = teacher.Education ?? "-";
                worksheet.Cells[row, 6].Value = teacher.Experience?.ToString() ?? "-";
                worksheet.Cells[row, 7].Value = teacher.User.TeacherClasses.Count;
                worksheet.Cells[row, 8].Value = teacher.User.CreatedTests.Count;
                worksheet.Cells[row, 9].Value = teacher.IsApproved ? "Одобрен" : "На модерации";
                worksheet.Cells[row, 10].Value = teacher.User.IsActive ? "Активен" : "Заблокирован";

                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportTeachersToCSVAsync()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.User.TeacherClasses)
                .Include(t => t.User.CreatedTests)
                .OrderBy(t => t.User.LastName)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;ФИО;Email;Предмет;Образование;Опыт;Классов;Тестов;Модерация;Статус");

            foreach (var teacher in teachers)
            {
                csv.AppendLine($"{teacher.Id};{teacher.User.FullName};{teacher.User.Email};" +
                    $"{teacher.Subject ?? "-"};{teacher.Education ?? "-"};{teacher.Experience?.ToString() ?? "-"};" +
                    $"{teacher.User.TeacherClasses.Count};{teacher.User.CreatedTests.Count};" +
                    $"{(teacher.IsApproved ? "Одобрен" : "На модерации")};" +
                    $"{(teacher.User.IsActive ? "Активен" : "Заблокирован")}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Students Export

        public async Task<byte[]> ExportStudentsToExcelAsync()
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Include(s => s.RegularTestResults)
                .Include(s => s.SpellingTestResults)
                .Include(s => s.PunctuationTestResults)
                .Include(s => s.OrthoeopyTestResults)
                .OrderBy(s => s.User.LastName)
                .ToListAsync();

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
                var completedTests = student.RegularTestResults.Count(r => r.IsCompleted) +
                                   student.SpellingTestResults.Count(r => r.IsCompleted) +
                                   student.PunctuationTestResults.Count(r => r.IsCompleted) +
                                   student.OrthoeopyTestResults.Count(r => r.IsCompleted);

                var allResults = new List<double>();
                allResults.AddRange(student.RegularTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));
                allResults.AddRange(student.SpellingTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));
                allResults.AddRange(student.PunctuationTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));
                allResults.AddRange(student.OrthoeopyTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));

                var avgScore = allResults.Any() ? allResults.Average() : 0;

                worksheet.Cells[row, 1].Value = student.Id;
                worksheet.Cells[row, 2].Value = student.User.FullName;
                worksheet.Cells[row, 3].Value = student.User.Email;
                worksheet.Cells[row, 4].Value = student.School ?? "-";
                worksheet.Cells[row, 5].Value = student.Grade?.ToString() ?? "-";
                worksheet.Cells[row, 6].Value = student.Class?.Name ?? "-";
                worksheet.Cells[row, 7].Value = completedTests;
                worksheet.Cells[row, 8].Value = $"{avgScore:F1}%";
                worksheet.Cells[row, 9].Value = student.User.IsActive ? "Активен" : "Заблокирован";

                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportStudentsToCSVAsync()
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Include(s => s.RegularTestResults)
                .Include(s => s.SpellingTestResults)
                .Include(s => s.PunctuationTestResults)
                .Include(s => s.OrthoeopyTestResults)
                .OrderBy(s => s.User.LastName)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;ФИО;Email;Школа;Класс в школе;Класс на платформе;Тестов пройдено;Средний балл;Статус");

            foreach (var student in students)
            {
                var completedTests = student.RegularTestResults.Count(r => r.IsCompleted) +
                                   student.SpellingTestResults.Count(r => r.IsCompleted) +
                                   student.PunctuationTestResults.Count(r => r.IsCompleted) +
                                   student.OrthoeopyTestResults.Count(r => r.IsCompleted);

                var allResults = new List<double>();
                allResults.AddRange(student.RegularTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));
                allResults.AddRange(student.SpellingTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));
                allResults.AddRange(student.PunctuationTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));
                allResults.AddRange(student.OrthoeopyTestResults.Where(r => r.IsCompleted).Select(r => r.Percentage));

                var avgScore = allResults.Any() ? allResults.Average() : 0;

                csv.AppendLine($"{student.Id};{student.User.FullName};{student.User.Email};" +
                    $"{student.School ?? "-"};{student.Grade?.ToString() ?? "-"};{student.Class?.Name ?? "-"};" +
                    $"{completedTests};{avgScore:F1}%;{(student.User.IsActive ? "Активен" : "Заблокирован")}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Classes Export

        public async Task<byte[]> ExportClassesToExcelAsync()
        {
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .Include(c => c.RegularTests)
                .Include(c => c.SpellingTests)
                .Include(c => c.PunctuationTests)
                .Include(c => c.OrthoeopyTests)
                .OrderBy(c => c.Name)
                .ToListAsync();

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
                var totalTests = classItem.RegularTests.Count + classItem.SpellingTests.Count +
                               classItem.PunctuationTests.Count + classItem.OrthoeopyTests.Count;

                worksheet.Cells[row, 1].Value = classItem.Id;
                worksheet.Cells[row, 2].Value = classItem.Name;
                worksheet.Cells[row, 3].Value = classItem.Description ?? "-";
                worksheet.Cells[row, 4].Value = classItem.Teacher.FullName;
                worksheet.Cells[row, 5].Value = classItem.Students.Count;
                worksheet.Cells[row, 6].Value = classItem.SpellingTests.Count;
                worksheet.Cells[row, 7].Value = classItem.RegularTests.Count;
                worksheet.Cells[row, 8].Value = classItem.PunctuationTests.Count;
                worksheet.Cells[row, 9].Value = classItem.OrthoeopyTests.Count;
                worksheet.Cells[row, 10].Value = totalTests;
                worksheet.Cells[row, 11].Value = classItem.CreatedAt.ToString("dd.MM.yyyy");

                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportClassesToCSVAsync()
        {
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .Include(c => c.RegularTests)
                .Include(c => c.SpellingTests)
                .Include(c => c.PunctuationTests)
                .Include(c => c.OrthoeopyTests)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;Название;Описание;Учитель;Студентов;Орфография;Классические;Пунктуация;Орфоэпия;Всего тестов;Дата создания");

            foreach (var classItem in classes)
            {
                var totalTests = classItem.RegularTests.Count + classItem.SpellingTests.Count +
                               classItem.PunctuationTests.Count + classItem.OrthoeopyTests.Count;

                csv.AppendLine($"{classItem.Id};{classItem.Name};{classItem.Description ?? "-"};" +
                    $"{classItem.Teacher.FullName};{classItem.Students.Count};" +
                    $"{classItem.SpellingTests.Count};{classItem.RegularTests.Count};" +
                    $"{classItem.PunctuationTests.Count};{classItem.OrthoeopyTests.Count};" +
                    $"{totalTests};{classItem.CreatedAt:dd.MM.yyyy}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Tests Export

        public async Task<byte[]> ExportTestsToExcelAsync()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Тесты");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Название";
            worksheet.Cells[1, 3].Value = "Тип";
            worksheet.Cells[1, 4].Value = "Учитель";
            worksheet.Cells[1, 5].Value = "Класс";
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

            int row = 2;

            // Орфография
            var spellingTests = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                .Include(st => st.SpellingQuestions)
                .Include(st => st.SpellingTestResults)
                .ToListAsync();

            foreach (var test in spellingTests)
            {
                worksheet.Cells[row, 1].Value = test.Id;
                worksheet.Cells[row, 2].Value = test.Title;
                worksheet.Cells[row, 3].Value = "Орфография";
                worksheet.Cells[row, 4].Value = test.Teacher.FullName;
                worksheet.Cells[row, 5].Value = test.Class?.Name ?? "Все ученики";
                worksheet.Cells[row, 6].Value = test.SpellingQuestions.Count;
                worksheet.Cells[row, 7].Value = test.SpellingTestResults.Count;
                worksheet.Cells[row, 8].Value = test.IsActive ? "Активен" : "Неактивен";
                worksheet.Cells[row, 9].Value = test.CreatedAt.ToString("dd.MM.yyyy");
                row++;
            }

            // Классические
            var regularTests = await _context.RegularTests
                .Include(t => t.Teacher)
                .Include(t => t.Class)
                .Include(t => t.RegularQuestions)
                .Include(t => t.RegularTestResults)
                .ToListAsync();

            foreach (var test in regularTests)
            {
                worksheet.Cells[row, 1].Value = test.Id;
                worksheet.Cells[row, 2].Value = test.Title;
                worksheet.Cells[row, 3].Value = "Классический";
                worksheet.Cells[row, 4].Value = test.Teacher.FullName;
                worksheet.Cells[row, 5].Value = test.Class?.Name ?? "Все ученики";
                worksheet.Cells[row, 6].Value = test.RegularQuestions.Count;
                worksheet.Cells[row, 7].Value = test.RegularTestResults.Count;
                worksheet.Cells[row, 8].Value = test.IsActive ? "Активен" : "Неактивен";
                worksheet.Cells[row, 9].Value = test.CreatedAt.ToString("dd.MM.yyyy");
                row++;
            }

            // Пунктуация
            var punctuationTests = await _context.PunctuationTests
                .Include(t => t.Teacher)
                .Include(t => t.Class)
                .Include(t => t.PunctuationQuestions)
                .Include(t => t.PunctuationTestResults)
                .ToListAsync();

            foreach (var test in punctuationTests)
            {
                worksheet.Cells[row, 1].Value = test.Id;
                worksheet.Cells[row, 2].Value = test.Title;
                worksheet.Cells[row, 3].Value = "Пунктуация";
                worksheet.Cells[row, 4].Value = test.Teacher.FullName;
                worksheet.Cells[row, 5].Value = test.Class?.Name ?? "Все ученики";
                worksheet.Cells[row, 6].Value = test.PunctuationQuestions.Count;
                worksheet.Cells[row, 7].Value = test.PunctuationTestResults.Count;
                worksheet.Cells[row, 8].Value = test.IsActive ? "Активен" : "Неактивен";
                worksheet.Cells[row, 9].Value = test.CreatedAt.ToString("dd.MM.yyyy");
                row++;
            }

            // Орфоэпия
            var orthoeopyTests = await _context.OrthoeopyTests
                .Include(t => t.Teacher)
                .Include(t => t.Class)
                .Include(t => t.OrthoeopyQuestions)
                .Include(t => t.OrthoeopyTestResults)
                .ToListAsync();

            foreach (var test in orthoeopyTests)
            {
                worksheet.Cells[row, 1].Value = test.Id;
                worksheet.Cells[row, 2].Value = test.Title;
                worksheet.Cells[row, 3].Value = "Орфоэпия";
                worksheet.Cells[row, 4].Value = test.Teacher.FullName;
                worksheet.Cells[row, 5].Value = test.Class?.Name ?? "Все ученики";
                worksheet.Cells[row, 6].Value = test.OrthoeopyQuestions.Count;
                worksheet.Cells[row, 7].Value = test.OrthoeopyTestResults.Count;
                worksheet.Cells[row, 8].Value = test.IsActive ? "Активен" : "Неактивен";
                worksheet.Cells[row, 9].Value = test.CreatedAt.ToString("dd.MM.yyyy");
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportTestsToCSVAsync()
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID;Название;Тип;Учитель;Класс;Вопросов;Результатов;Статус;Дата создания");

            // Аналогично Excel, но в CSV формате
            var spellingTests = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                .Include(st => st.SpellingQuestions)
                .Include(st => st.SpellingTestResults)
                .ToListAsync();

            foreach (var test in spellingTests)
            {
                csv.AppendLine($"{test.Id};{test.Title};Орфография;{test.Teacher.FullName};" +
                    $"{test.Class?.Name ?? "Все ученики"};{test.SpellingQuestions.Count};" +
                    $"{test.SpellingTestResults.Count};{(test.IsActive ? "Активен" : "Неактивен")};" +
                    $"{test.CreatedAt:dd.MM.yyyy}");
            }

            // Добавьте аналогично для других типов тестов...

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Test Results Export

        public async Task<byte[]> ExportTestResultsToExcelAsync()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Результаты тестов");

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

            int row = 2;

            // Орфография
            var spellingResults = await _context.SpellingTestResults
                .Include(r => r.SpellingTest)
                .Include(r => r.Student).ThenInclude(s => s.User)
                .ToListAsync();

            foreach (var result in spellingResults)
            {
                worksheet.Cells[row, 1].Value = result.Id;
                worksheet.Cells[row, 2].Value = result.SpellingTest.Title;
                worksheet.Cells[row, 3].Value = "Орфография";
                worksheet.Cells[row, 4].Value = result.Student.User.FullName;
                worksheet.Cells[row, 5].Value = result.Score;
                worksheet.Cells[row, 6].Value = result.MaxScore;
                worksheet.Cells[row, 7].Value = $"{result.Percentage:F1}%";
                worksheet.Cells[row, 8].Value = result.StartedAt.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 9].Value = result.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
                worksheet.Cells[row, 10].Value = result.IsCompleted ? "Завершен" : "В процессе";
                row++;
            }

            // Добавьте аналогично для других типов...

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportTestResultsToCSVAsync()
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID;Тест;Тип;Студент;Баллов;Макс. баллов;Процент;Начало;Завершение;Статус");

            var spellingResults = await _context.SpellingTestResults
                .Include(r => r.SpellingTest)
                .Include(r => r.Student).ThenInclude(s => s.User)
                .ToListAsync();

            foreach (var result in spellingResults)
            {
                csv.AppendLine($"{result.Id};{result.SpellingTest.Title};Орфография;" +
                    $"{result.Student.User.FullName};{result.Score};{result.MaxScore};" +
                    $"{result.Percentage:F1}%;{result.StartedAt:dd.MM.yyyy HH:mm};" +
                    $"{result.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"};" +
                    $"{(result.IsCompleted ? "Завершен" : "В процессе")}");
            }

            // Добавьте аналогично для других типов...

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Audit Logs Export

        public async Task<byte[]> ExportAuditLogsToExcelAsync()
        {
            var logs = await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.CreatedAt)
                .Take(10000) // Ограничим 10000 записями
                .ToListAsync();

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
                worksheet.Cells[row, 1].Value = log.Id;
                worksheet.Cells[row, 2].Value = log.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
                worksheet.Cells[row, 3].Value = log.UserName;
                worksheet.Cells[row, 4].Value = log.User?.Email ?? "-";
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
            var logs = await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.CreatedAt)
                .Take(10000)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID;Дата и время;Администратор;Email;Действие;Тип сущности;ID сущности;Детали;IP адрес");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id};{log.CreatedAt:dd.MM.yyyy HH:mm:ss};{log.UserName};" +
                    $"{log.User?.Email ?? "-"};{log.Action};{log.EntityType};" +
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
