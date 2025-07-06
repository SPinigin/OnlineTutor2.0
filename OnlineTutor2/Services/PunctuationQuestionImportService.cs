using OfficeOpenXml;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Services
{
    public interface IPunctuationQuestionImportService
    {
        Task<List<ImportPunctuationQuestionRow>> ParseExcelFileAsync(IFormFile file);
        Task<byte[]> GenerateTemplateAsync();
    }

    public class PunctuationQuestionImportService : IPunctuationQuestionImportService
    {
        private void ConfigureExcelPackage()
        {
            try
            {
                if (ExcelPackage.LicenseContext == LicenseContext.Commercial)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                }
                Console.WriteLine($"[DEBUG] EPPlus license context: {ExcelPackage.LicenseContext}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ConfigureExcelPackage error: {ex.Message}");
            }
        }

        public async Task<List<ImportPunctuationQuestionRow>> ParseExcelFileAsync(IFormFile file)
        {
            var questions = new List<ImportPunctuationQuestionRow>();

            try
            {
                ConfigureExcelPackage();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);

                if (package.Workbook?.Worksheets?.Count == 0)
                {
                    throw new InvalidOperationException("Excel файл не содержит листов");
                }

                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                {
                    return questions;
                }

                var rowCount = worksheet.Dimension.Rows;

                if (rowCount < 2)
                {
                    throw new InvalidOperationException("Excel файл должен содержать заголовки и хотя бы одну строку данных");
                }

                // Читаем данные начиная со 2-й строки
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var question = new ImportPunctuationQuestionRow { RowNumber = row };

                        question.SentenceWithNumbers = GetCellValue(worksheet, row, 1);
                        question.CorrectPositions = GetCellValue(worksheet, row, 2);
                        question.PlainSentence = GetCellValue(worksheet, row, 3);
                        question.Hint = GetCellValue(worksheet, row, 4);

                        // Пропускаем пустые строки
                        if (string.IsNullOrWhiteSpace(question.SentenceWithNumbers) &&
                            string.IsNullOrWhiteSpace(question.CorrectPositions))
                        {
                            continue;
                        }

                        ValidateQuestion(question);
                        questions.Add(question);
                    }
                    catch (Exception rowEx)
                    {
                        var errorQuestion = new ImportPunctuationQuestionRow
                        {
                            RowNumber = row,
                            SentenceWithNumbers = GetCellValue(worksheet, row, 1) ?? "",
                            CorrectPositions = GetCellValue(worksheet, row, 2) ?? ""
                        };
                        errorQuestion.Errors.Add($"Ошибка обработки строки: {rowEx.Message}");
                        questions.Add(errorQuestion);
                    }
                }

                return questions;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при чтении Excel файла: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateTemplateAsync()
        {
            ConfigureExcelPackage();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Вопросы на пунктуацию");

            // Заголовки
            worksheet.Cells[1, 1].Value = "Предложение с номерами*";
            worksheet.Cells[1, 2].Value = "Правильные позиции*";
            worksheet.Cells[1, 3].Value = "Предложение без номеров";
            worksheet.Cells[1, 4].Value = "Подсказка";

            // Примеры данных
            worksheet.Cells[2, 1].Value = "Когда солнце взошло(1) птицы запели(2) а цветы раскрылись(3) наступил новый день.";
            worksheet.Cells[2, 2].Value = "13";
            worksheet.Cells[2, 3].Value = "Когда солнце взошло, птицы запели, а цветы раскрылись, наступил новый день.";
            worksheet.Cells[2, 4].Value = "Запятые ставятся для выделения однородных членов и обстоятельственных оборотов.";

            worksheet.Cells[3, 1].Value = "Если завтра будет дождь(1) мы останемся дома(2) но если погода наладится(3) пойдем в парк.";
            worksheet.Cells[3, 2].Value = "123";
            worksheet.Cells[3, 3].Value = "Если завтра будет дождь, мы останемся дома, но если погода наладится, пойдем в парк.";
            worksheet.Cells[3, 4].Value = "Запятые разделяют части сложного предложения.";

            worksheet.Cells[4, 1].Value = "Мария(1) которая работает учителем(2) очень любит детей.";
            worksheet.Cells[4, 2].Value = "12";
            worksheet.Cells[4, 3].Value = "Мария, которая работает учителем, очень любит детей.";
            worksheet.Cells[4, 4].Value = "Запятые выделяют придаточное определительное предложение.";

            // Форматирование заголовков
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cells[1, i].Style.Font.Bold = true;
            }

            // Настройка ширины столбцов
            worksheet.Column(1).Width = 60; // Предложение с номерами
            worksheet.Column(2).Width = 20; // Правильные позиции
            worksheet.Column(3).Width = 60; // Предложение без номеров
            worksheet.Column(4).Width = 50; // Подсказка

            // Добавляем лист с инструкциями
            var instructionSheet = package.Workbook.Worksheets.Add("Инструкция");
            instructionSheet.Cells[1, 1].Value = "Инструкция по заполнению вопросов на пунктуацию";
            instructionSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionSheet.Cells[1, 1].Style.Font.Size = 14;

            instructionSheet.Cells[3, 1].Value = "Формат заполнения:";
            instructionSheet.Cells[3, 1].Style.Font.Bold = true;
            instructionSheet.Cells[4, 1].Value = "1. Предложение с номерами - используйте символы (1) (2) (3) (4) (5) (6) (7) (8) (9) для обозначения мест где могут быть запятые";
            instructionSheet.Cells[5, 1].Value = "2. Правильные позиции - укажите номера через запятую где должны стоять запятые (например: 135)";
            instructionSheet.Cells[6, 1].Value = "3. Предложение без номеров - то же предложение с правильно расставленными запятыми";
            instructionSheet.Cells[7, 1].Value = "4. Подсказка - объяснение правила пунктуации (необязательно)";

            instructionSheet.Cells[9, 1].Value = "Примеры номеров:";
            instructionSheet.Cells[9, 1].Style.Font.Bold = true;
            instructionSheet.Cells[10, 1].Value = "(1) (2) (3) (4) (5) (6) (7) (8) (9) (скопируйте нужные символы)";

            instructionSheet.Cells[12, 1].Value = "Важно:";
            instructionSheet.Cells[12, 1].Style.Font.Bold = true;
            instructionSheet.Cells[13, 1].Value = "• Не удаляйте строку с заголовками";
            instructionSheet.Cells[14, 1].Value = "• Заполняйте данные начиная со 2-й строки";
            instructionSheet.Cells[15, 1].Value = "• Номера позиций указывайте через запятую без пробелов";
            instructionSheet.Cells[16, 1].Value = "• Если запятых не нужно, оставьте поле позиций пустым";

            instructionSheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                return cell?.Value?.ToString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private void ValidateQuestion(ImportPunctuationQuestionRow question)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(question.SentenceWithNumbers))
                question.Errors.Add("Предложение с номерами обязательно");

            if (string.IsNullOrWhiteSpace(question.CorrectPositions))
                question.Errors.Add("Правильные позиции обязательны");

            // Проверка формата позиций
            if (!string.IsNullOrWhiteSpace(question.CorrectPositions))
            {
                // Проверяем, что строка содержит только цифры от 1 до 9
                foreach (char c in question.CorrectPositions)
                {
                    if (!char.IsDigit(c) || c < '1' || c > '9')
                    {
                        question.Errors.Add($"Неверный формат позиций: {question.CorrectPositions}. Используйте только цифры от 1 до 9 без пробелов и знаков препинания (например: 135)");
                        break;
                    }
                }

                // Проверяем на дублирование позиций
                var uniquePositions = question.CorrectPositions.Distinct().Count();
                if (uniquePositions != question.CorrectPositions.Length)
                {
                    question.Errors.Add($"Обнаружены дублирующиеся позиции в: {question.CorrectPositions}");
                }
            }

            // Проверка наличия номеров в предложении
            if (!string.IsNullOrWhiteSpace(question.SentenceWithNumbers))
            {
                var superscriptNumbers = new[] { "(1)", "(2)", "(3)", "(4)", "(5)", "(6)", "(7)", "(8)", "(9)" };
                bool hasNumbers = superscriptNumbers.Any(num => question.SentenceWithNumbers.Contains(num));

                if (!hasNumbers)
                {
                    question.Errors.Add("Предложение должно содержать номера позиций (1) (2) (3) (4) (5) (6) (7) (8) (9)");
                }
            }
        }
    }
}
