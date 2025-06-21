using OfficeOpenXml;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Services
{
    public interface ISpellingQuestionImportService
    {
        Task<List<ImportQuestionRow>> ParseExcelFileAsync(IFormFile file);
        Task<byte[]> GenerateTemplateAsync();
    }

    public class SpellingQuestionImportService : ISpellingQuestionImportService
    {
        private void ConfigureExcelPackage()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            }
            catch { }
        }

        public async Task<List<ImportQuestionRow>> ParseExcelFileAsync(IFormFile file)
        {
            ConfigureExcelPackage();

            var questions = new List<ImportQuestionRow>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null) return questions;

            var rowCount = worksheet.Dimension.Rows;

            // Начинаем с 2 строки (1-я строка - заголовки)
            for (int row = 2; row <= rowCount; row++)
            {
                var question = new ImportQuestionRow { RowNumber = row };

                // Читаем данные из столбцов
                question.WordWithGap = GetCellValue(worksheet, row, 1);      // Слово (с пропуском)
                question.CorrectLetter = GetCellValue(worksheet, row, 2);    // Правильная буква
                question.FullWord = GetCellValue(worksheet, row, 3);         // Полное слово
                question.Hint = GetCellValue(worksheet, row, 4);             // Подсказка

                // Пропускаем пустые строки
                if (string.IsNullOrWhiteSpace(question.WordWithGap) &&
                    string.IsNullOrWhiteSpace(question.CorrectLetter) &&
                    string.IsNullOrWhiteSpace(question.FullWord))
                {
                    continue;
                }

                // Валидация
                ValidateQuestion(question);
                questions.Add(question);
            }

            return questions;
        }

        public async Task<byte[]> GenerateTemplateAsync()
        {
            ConfigureExcelPackage();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Вопросы");

            // Заголовки
            worksheet.Cells[1, 1].Value = "Слово (с пропуском)*";
            worksheet.Cells[1, 2].Value = "Правильная буква*";
            worksheet.Cells[1, 3].Value = "Полное слово*";
            worksheet.Cells[1, 4].Value = "Подсказка";

            // Примеры данных из вашего файла
            worksheet.Cells[2, 1].Value = "Прол…тает";
            worksheet.Cells[2, 2].Value = "е";
            worksheet.Cells[2, 3].Value = "пролетает";
            worksheet.Cells[2, 4].Value = "Безударная гласная \"е\" в корне слова \"пролетает\" проверяется подбором проверочного слова, где эта гласная находится под ударением.";

            worksheet.Cells[3, 1].Value = "пож…лтели";
            worksheet.Cells[3, 2].Value = "е";
            worksheet.Cells[3, 3].Value = "пожелтели";
            worksheet.Cells[3, 4].Value = "Безударная гласная \"е\" в корне слова \"пожелтели\" проверяется подбором проверочного слова \"жёлтый\".";

            worksheet.Cells[4, 1].Value = "сн…говик";
            worksheet.Cells[4, 2].Value = "е";
            worksheet.Cells[4, 3].Value = "снеговик";
            worksheet.Cells[4, 4].Value = "Безударная гласная \"е\" в корне слова \"снеговик\" проверяется с помощью слова \"снег\".";

            // Форматирование заголовков
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cells[1, i].Style.Font.Bold = true;
            }

            // Настройка ширины столбцов
            worksheet.Column(1).Width = 20; // Слово с пропуском
            worksheet.Column(2).Width = 15; // Правильная буква
            worksheet.Column(3).Width = 20; // Полное слово
            worksheet.Column(4).Width = 50; // Подсказка

            // Добавляем лист с инструкциями
            var instructionSheet = package.Workbook.Worksheets.Add("Инструкция");
            instructionSheet.Cells[1, 1].Value = "Инструкция по заполнению вопросов";
            instructionSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionSheet.Cells[1, 1].Style.Font.Size = 14;

            instructionSheet.Cells[3, 1].Value = "Формат заполнения:";
            instructionSheet.Cells[3, 1].Style.Font.Bold = true;
            instructionSheet.Cells[4, 1].Value = "1. Слово с пропуском - используйте символ … для обозначения пропущенной буквы";
            instructionSheet.Cells[5, 1].Value = "2. Правильная буква - одна или несколько букв (а, е, и, о, у, я, ё)";
            instructionSheet.Cells[6, 1].Value = "3. Полное слово - слово без пропусков";
            instructionSheet.Cells[7, 1].Value = "4. Подсказка - объяснение правила (необязательно)";

            instructionSheet.Cells[9, 1].Value = "Примеры:";
            instructionSheet.Cells[9, 1].Style.Font.Bold = true;
            instructionSheet.Cells[10, 1].Value = "• д…ждливый | о | дождливый | Проверочное слово: дождь";
            instructionSheet.Cells[11, 1].Value = "• л…сник | е | лесник | Проверочное слово: лес";
            instructionSheet.Cells[12, 1].Value = "• ст…р…жил | о,о | сторожил | Проверочное слово: сторож";

            instructionSheet.Cells[14, 1].Value = "Важно:";
            instructionSheet.Cells[14, 1].Style.Font.Bold = true;
            instructionSheet.Cells[15, 1].Value = "• Не удаляйте строку с заголовками";
            instructionSheet.Cells[16, 1].Value = "• Заполняйте данные начиная со 2-й строки";
            instructionSheet.Cells[17, 1].Value = "• Для нескольких пропусков используйте запятую: а,о";

            instructionSheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

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

        private void ValidateQuestion(ImportQuestionRow question)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(question.WordWithGap))
                question.Errors.Add("Слово с пропуском обязательно");
            else if (!question.WordWithGap.Contains("…"))
                question.Errors.Add("Слово должно содержать символ пропуска (…)");

            if (string.IsNullOrWhiteSpace(question.CorrectLetter))
                question.Errors.Add("Правильная буква обязательна");

            if (string.IsNullOrWhiteSpace(question.FullWord))
                question.Errors.Add("Полное слово обязательно");

            // Проверка соответствия
            if (!string.IsNullOrWhiteSpace(question.WordWithGap) &&
                !string.IsNullOrWhiteSpace(question.FullWord) &&
                !string.IsNullOrWhiteSpace(question.CorrectLetter))
            {
                // Простая проверка - замещаем … на правильную букву и сравниваем
                var reconstructed = question.WordWithGap.Replace("…", question.CorrectLetter);
                if (!reconstructed.Equals(question.FullWord, StringComparison.OrdinalIgnoreCase))
                {
                    question.Errors.Add("Полное слово не соответствует слову с заменёнными буквами");
                }
            }
        }
    }
}
