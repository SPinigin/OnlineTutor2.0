using OfficeOpenXml;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Services
{
    public interface IStudentImportService
    {
        Task<List<ImportStudentRow>> ParseExcelFileAsync(IFormFile file, bool autoGeneratePasswords, string? defaultPassword);
        Task<byte[]> GenerateTemplateAsync();
    }

    public class StudentImportService : IStudentImportService
    {
        private void ConfigureExcelPackage()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ConfigureExcelPackage error: {ex.Message}");
            }
        }

        public async Task<List<ImportStudentRow>> ParseExcelFileAsync(IFormFile file, bool autoGeneratePasswords, string? defaultPassword)
        {
            ConfigureExcelPackage();

            var students = new List<ImportStudentRow>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null) return students;

            var rowCount = worksheet.Dimension.Rows;

            // Начинаем с 2 строки (1-я строка - заголовки)
            for (int row = 2; row <= rowCount; row++)
            {
                var student = new ImportStudentRow { RowNumber = row };

                // Читаем данные из столбцов
                student.FirstName = GetCellValue(worksheet, row, 1);
                student.LastName = GetCellValue(worksheet, row, 2);
                student.Email = GetCellValue(worksheet, row, 3);

                // Дата рождения
                if (DateTime.TryParse(GetCellValue(worksheet, row, 4), out var dateOfBirth))
                {
                    student.DateOfBirth = dateOfBirth;
                }

                student.PhoneNumber = GetCellValue(worksheet, row, 5);
                student.School = GetCellValue(worksheet, row, 6);

                // Класс
                if (int.TryParse(GetCellValue(worksheet, row, 7), out var grade) && grade >= 1 && grade <= 11)
                {
                    student.Grade = grade;
                }

                // Пароль
                var passwordFromFile = GetCellValue(worksheet, row, 8);
                if (!string.IsNullOrEmpty(passwordFromFile))
                {
                    student.Password = passwordFromFile;
                }
                else if (autoGeneratePasswords)
                {
                    student.Password = GeneratePassword();
                }
                else
                {
                    student.Password = defaultPassword ?? "Student123";
                }

                // Пропускаем пустые строки
                if (string.IsNullOrWhiteSpace(student.FirstName) &&
                    string.IsNullOrWhiteSpace(student.LastName) &&
                    string.IsNullOrWhiteSpace(student.Email))
                {
                    continue;
                }

                // Простая валидация
                ValidateStudent(student);
                students.Add(student);
            }

            return students;
        }

        public async Task<byte[]> GenerateTemplateAsync()
        {
            ConfigureExcelPackage();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Шаблон");

            // Заголовки
            worksheet.Cells[1, 1].Value = "Имя*";
            worksheet.Cells[1, 2].Value = "Фамилия*";
            worksheet.Cells[1, 3].Value = "Email*";
            worksheet.Cells[1, 4].Value = "Дата рождения*";
            worksheet.Cells[1, 5].Value = "Телефон";
            worksheet.Cells[1, 6].Value = "Школа";
            worksheet.Cells[1, 7].Value = "Класс (1-11)";
            worksheet.Cells[1, 8].Value = "Пароль";

            // Пример данных
            worksheet.Cells[2, 1].Value = "Иван";
            worksheet.Cells[2, 2].Value = "Иванов";
            worksheet.Cells[2, 3].Value = "ivan@example.com";
            worksheet.Cells[2, 4].Value = "15.05.2007";
            worksheet.Cells[2, 5].Value = "+7 999 123-45-67";
            worksheet.Cells[2, 6].Value = "Школа №1";
            worksheet.Cells[2, 7].Value = 10;
            worksheet.Cells[2, 8].Value = "MyPassword123";

            // Второй пример
            worksheet.Cells[3, 1].Value = "Мария";
            worksheet.Cells[3, 2].Value = "Петрова";
            worksheet.Cells[3, 3].Value = "maria@example.com";
            worksheet.Cells[3, 4].Value = "22.08.2006";
            worksheet.Cells[3, 5].Value = "+7 999 987-65-43";
            worksheet.Cells[3, 6].Value = "Гимназия №5";
            worksheet.Cells[3, 7].Value = 11;
            worksheet.Cells[3, 8].Value = "";  // Пустой пароль - будет сгенерирован

            // Простое форматирование
            for (int i = 1; i <= 8; i++)
            {
                worksheet.Cells[1, i].Style.Font.Bold = true;
            }

            // Автоширина столбцов
            worksheet.Cells.AutoFitColumns();

            // Добавляем простые инструкции
            var instructionSheet = package.Workbook.Worksheets.Add("Инструкция");
            instructionSheet.Cells[1, 1].Value = "Инструкция по заполнению";
            instructionSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionSheet.Cells[1, 1].Style.Font.Size = 14;

            instructionSheet.Cells[3, 1].Value = "Обязательные поля (помечены *)";
            instructionSheet.Cells[4, 1].Value = "• Имя и Фамилия ученика";
            instructionSheet.Cells[5, 1].Value = "• Email (должен быть уникальным)";
            instructionSheet.Cells[6, 1].Value = "• Дата рождения (в формате ДД.ММ.ГГГГ)";

            instructionSheet.Cells[8, 1].Value = "Необязательные поля";
            instructionSheet.Cells[9, 1].Value = "• Телефон, Школа, Класс";
            instructionSheet.Cells[10, 1].Value = "• Пароль (если пустой - будет сгенерирован)";

            instructionSheet.Cells[12, 1].Value = "Важно:";
            instructionSheet.Cells[12, 1].Style.Font.Bold = true;
            instructionSheet.Cells[13, 1].Value = "• Не удаляйте строку с заголовками";
            instructionSheet.Cells[14, 1].Value = "• Заполняйте данные начиная со 2-й строки";
            instructionSheet.Cells[15, 1].Value = "• Email должен быть уникальным";

            instructionSheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        // Вспомогательные методы
        private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                return worksheet.Cells[row, col].Value?.ToString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private void ValidateStudent(ImportStudentRow student)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(student.FirstName))
                student.Errors.Add("Имя обязательно");

            if (string.IsNullOrWhiteSpace(student.LastName))
                student.Errors.Add("Фамилия обязательна");

            if (string.IsNullOrWhiteSpace(student.Email))
                student.Errors.Add("Email обязателен");
            else if (!student.Email.Contains("@"))
                student.Errors.Add("Неверный формат email");

            if (!student.DateOfBirth.HasValue)
                student.Errors.Add("Дата рождения обязательна");

            if (string.IsNullOrWhiteSpace(student.Password))
                student.Errors.Add("Пароль обязателен");
            else if (student.Password.Length < 6)
                student.Errors.Add("Пароль должен быть минимум 6 символов");
        }

        private string GeneratePassword()
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
