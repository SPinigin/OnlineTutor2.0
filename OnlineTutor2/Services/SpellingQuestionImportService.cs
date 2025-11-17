using OfficeOpenXml;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Services
{
    public interface ISpellingQuestionImportService
    {
        Task<List<ImportSpellingQuestionRow>> ParseExcelFileAsync(IFormFile file);
        Task<byte[]> GenerateTemplateAsync();
    }

    public class SpellingQuestionImportService : ISpellingQuestionImportService
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
                // Не прерываем выполнение
            }
        }

        public async Task<List<ImportSpellingQuestionRow>> ParseExcelFileAsync(IFormFile file)
        {
            var questions = new List<ImportSpellingQuestionRow>();

            try
            {
                Console.WriteLine("[DEBUG] Configuring Excel package...");
                ConfigureExcelPackage();

                Console.WriteLine("[DEBUG] Creating memory stream...");
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                Console.WriteLine($"[DEBUG] Stream created, length: {stream.Length}");

                Console.WriteLine("[DEBUG] Creating Excel package...");
                using var package = new ExcelPackage(stream);

                if (package.Workbook == null)
                {
                    throw new InvalidOperationException("Не удалось открыть Excel файл - workbook is null");
                }

                Console.WriteLine($"[DEBUG] Workbook created, worksheets count: {package.Workbook.Worksheets.Count}");

                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("Excel файл не содержит листов");
                }

                var worksheet = package.Workbook.Worksheets[0];
                Console.WriteLine($"[DEBUG] Using worksheet: {worksheet.Name}");

                if (worksheet.Dimension == null)
                {
                    Console.WriteLine("[DEBUG] Worksheet dimension is null, returning empty list");
                    return questions;
                }

                var rowCount = worksheet.Dimension.Rows;
                var colCount = worksheet.Dimension.Columns;
                Console.WriteLine($"[DEBUG] Worksheet dimensions: {rowCount} rows, {colCount} columns");

                if (rowCount < 2)
                {
                    throw new InvalidOperationException("Excel файл должен содержать заголовки и хотя бы одну строку данных");
                }

                // Читаем заголовки для отладки
                Console.WriteLine("[DEBUG] Headers:");
                for (int col = 1; col <= Math.Min(colCount, 4); col++)
                {
                    var header = GetCellValue(worksheet, 1, col);
                    Console.WriteLine($"  Column {col}: '{header}'");
                }

                // Читаем данные
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var question = new ImportSpellingQuestionRow { RowNumber = row };

                        question.WordWithGap = GetCellValue(worksheet, row, 1);
                        question.CorrectLetter = GetCellValue(worksheet, row, 2);
                        question.FullWord = GetCellValue(worksheet, row, 3);
                        question.Hint = GetCellValue(worksheet, row, 4);

                        Console.WriteLine($"[DEBUG] Row {row}: '{question.WordWithGap}' | '{question.CorrectLetter}' | '{question.FullWord}'");

                        // Пропускаем пустые строки
                        if (string.IsNullOrWhiteSpace(question.WordWithGap) &&
                            string.IsNullOrWhiteSpace(question.CorrectLetter) &&
                            string.IsNullOrWhiteSpace(question.FullWord))
                        {
                            Console.WriteLine($"[DEBUG] Skipping empty row {row}");
                            continue;
                        }

                        ValidateQuestion(question);
                        questions.Add(question);
                    }
                    catch (Exception rowEx)
                    {
                        Console.WriteLine($"[ERROR] Error processing row {row}: {rowEx.Message}");
                        var errorQuestion = new ImportSpellingQuestionRow
                        {
                            RowNumber = row,
                            WordWithGap = GetCellValue(worksheet, row, 1) ?? "",
                            CorrectLetter = GetCellValue(worksheet, row, 2) ?? "",
                            FullWord = GetCellValue(worksheet, row, 3) ?? ""
                        };
                        errorQuestion.Errors.Add($"Ошибка обработки строки: {rowEx.Message}");
                        questions.Add(errorQuestion);
                    }
                }

                Console.WriteLine($"[DEBUG] Parsing completed successfully. Total questions: {questions.Count}");
                return questions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ParseExcelFileAsync error: {ex}");
                throw new InvalidOperationException($"Ошибка при чтении Excel файла: {ex.Message}", ex);
            }
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
                var cell = worksheet.Cells[row, col];
                return cell?.Value?.ToString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private void ValidateQuestion(ImportSpellingQuestionRow question)
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
                // Подсчитываем количество пропусков в слове
                int gapCount = question.WordWithGap.Count(c => c == '…');

                // Разбиваем правильные буквы по запятой
                var correctLetters = question.CorrectLetter.Split(',')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToArray();

                // Проверяем, что количество букв соответствует количеству пропусков
                if (correctLetters.Length != gapCount)
                {
                    question.Errors.Add($"Количество букв ({correctLetters.Length}) не соответствует количеству пропусков ({gapCount})");
                    return;
                }

                // Заменяем пропуски по очереди
                var reconstructed = question.WordWithGap;
                foreach (var letter in correctLetters)
                {
                    int index = reconstructed.IndexOf('…');
                    if (index >= 0)
                    {
                        reconstructed = reconstructed.Remove(index, 1).Insert(index, letter);
                    }
                }

                // Сравниваем результат с полным словом
                if (!reconstructed.Equals(question.FullWord, StringComparison.OrdinalIgnoreCase))
                {
                    question.Errors.Add("Полное слово не соответствует слову с заменённым буквами");
                }
            }
        }
    }
}
