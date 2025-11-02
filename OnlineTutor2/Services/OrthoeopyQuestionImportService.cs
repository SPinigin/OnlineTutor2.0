using OfficeOpenXml;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Services
{
    public interface IOrthoeopyQuestionImportService
    {
        Task<List<ImportOrthoeopyQuestionRow>> ParseExcelFileAsync(IFormFile file);
        Task<byte[]> GenerateTemplateAsync();
    }

    public class OrthoeopyQuestionImportService : IOrthoeopyQuestionImportService
    {
        private readonly ILogger<OrthoeopyQuestionImportService> _logger;

        public OrthoeopyQuestionImportService(ILogger<OrthoeopyQuestionImportService> logger)
        {
            _logger = logger;
        }

        private void ConfigureExcelPackage()
        {
            try
            {
                if (ExcelPackage.LicenseContext == LicenseContext.Commercial)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                }
                _logger.LogDebug("EPPlus license context: {LicenseContext}", ExcelPackage.LicenseContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка настройки EPPlus license");
            }
        }

        public async Task<List<ImportOrthoeopyQuestionRow>> ParseExcelFileAsync(IFormFile file)
        {
            var questions = new List<ImportOrthoeopyQuestionRow>();

            try
            {
                _logger.LogInformation("Начало парсинга файла импорта вопросов орфоэпии. Файл: {FileName}, Размер: {FileSize} байт", file.FileName, file.Length);
                ConfigureExcelPackage();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);

                if (package.Workbook?.Worksheets?.Count == 0)
                {
                    _logger.LogWarning("Excel файл не содержит листов. Файл: {FileName}", file.FileName);
                    throw new InvalidOperationException("Excel файл не содержит листов");
                }

                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                {
                    _logger.LogWarning("Excel файл пустой. Файл: {FileName}", file.FileName);
                    return questions;
                }

                var rowCount = worksheet.Dimension.Rows;
                _logger.LogDebug("Найдено {RowCount} строк в файле {FileName}", rowCount, file.FileName);

                if (rowCount < 2)
                {
                    _logger.LogWarning("Excel файл содержит только заголовки. Файл: {FileName}", file.FileName);
                    throw new InvalidOperationException("Excel файл должен содержать заголовки и хотя бы одну строку данных");
                }

                // Читаем данные начиная со 2-й строки
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var question = new ImportOrthoeopyQuestionRow { RowNumber = row };

                        question.Word = GetCellValue(worksheet, row, 1);

                        var stressPositionValue = GetCellValue(worksheet, row, 2);
                        if (int.TryParse(stressPositionValue, out int stressPos))
                        {
                            question.StressPosition = stressPos;
                        }
                        else if (!string.IsNullOrWhiteSpace(stressPositionValue))
                        {
                            question.Errors.Add($"Позиция ударения должна быть числом: '{stressPositionValue}'");
                        }

                        question.WordWithStress = GetCellValue(worksheet, row, 3);
                        question.Hint = GetCellValue(worksheet, row, 4);

                        // Пропускаем пустые строки
                        if (string.IsNullOrWhiteSpace(question.Word) &&
                            string.IsNullOrWhiteSpace(question.WordWithStress))
                        {
                            continue;
                        }

                        ValidateQuestion(question);
                        questions.Add(question);
                    }
                    catch (Exception rowEx)
                    {
                        _logger.LogWarning(rowEx, "Ошибка обработки строки {RowNumber} в файле {FileName}", row, file.FileName);
                        var errorQuestion = new ImportOrthoeopyQuestionRow
                        {
                            RowNumber = row,
                            Word = GetCellValue(worksheet, row, 1) ?? "",
                            WordWithStress = GetCellValue(worksheet, row, 3) ?? ""
                        };
                        errorQuestion.Errors.Add($"Ошибка обработки строки: {rowEx.Message}");
                        questions.Add(errorQuestion);
                    }
                }

                var validCount = questions.Count(q => q.IsValid);
                var invalidCount = questions.Count(q => !q.IsValid);
                _logger.LogInformation("Парсинг файла {FileName} завершен. Всего строк: {TotalCount}, Валидных: {ValidCount}, Невалидных: {InvalidCount}",
                    file.FileName, questions.Count, validCount, invalidCount);

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка парсинга файла импорта вопросов орфоэпии. Файл: {FileName}", file.FileName);
                throw new InvalidOperationException($"Ошибка при чтении Excel файла: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateTemplateAsync()
        {
            _logger.LogInformation("Генерация шаблона импорта вопросов орфоэпии");
            ConfigureExcelPackage();

            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Вопросы на орфоэпию");

                // Заголовки
                worksheet.Cells[1, 1].Value = "Слово*";
                worksheet.Cells[1, 2].Value = "Позиция ударения*";
                worksheet.Cells[1, 3].Value = "Слово с ударением*";
                worksheet.Cells[1, 4].Value = "Подсказка";

                // Примеры данных
                worksheet.Cells[2, 1].Value = "договор";
                worksheet.Cells[2, 2].Value = 3;
                worksheet.Cells[2, 3].Value = "договóр";
                worksheet.Cells[2, 4].Value = "Ударение на последний слог";

                worksheet.Cells[3, 1].Value = "каталог";
                worksheet.Cells[3, 2].Value = 3;
                worksheet.Cells[3, 3].Value = "каталóг";
                worksheet.Cells[3, 4].Value = "Ударение на последний слог";

                worksheet.Cells[4, 1].Value = "звонит";
                worksheet.Cells[4, 2].Value = 2;
                worksheet.Cells[4, 3].Value = "звон́ит";
                worksheet.Cells[4, 4].Value = "От слова 'звон'";

                worksheet.Cells[5, 1].Value = "красивее";
                worksheet.Cells[5, 2].Value = 2;
                worksheet.Cells[5, 3].Value = "крас́ивее";
                worksheet.Cells[5, 4].Value = "Сравнительная степень";

                worksheet.Cells[6, 1].Value = "торты";
                worksheet.Cells[6, 2].Value = 1;
                worksheet.Cells[6, 3].Value = "т́орты";
                worksheet.Cells[6, 4].Value = "Множественное число от 'торт'";

                worksheet.Cells[7, 1].Value = "средства";
                worksheet.Cells[7, 2].Value = 1;
                worksheet.Cells[7, 3].Value = "ср́едства";
                worksheet.Cells[7, 4].Value = "Ударение на первый слог";

                worksheet.Cells[8, 1].Value = "включит";
                worksheet.Cells[8, 2].Value = 2;
                worksheet.Cells[8, 3].Value = "включ́ит";
                worksheet.Cells[8, 4].Value = "Глагол будущего времени";

                // Форматирование заголовков
                for (int i = 1; i <= 4; i++)
                {
                    worksheet.Cells[1, i].Style.Font.Bold = true;
                }

                // Настройка ширины столбцов
                worksheet.Column(1).Width = 30; // Слово
                worksheet.Column(2).Width = 20; // Позиция ударения
                worksheet.Column(3).Width = 30; // Слово с ударением
                worksheet.Column(4).Width = 50; // Подсказка

                // Добавляем лист с инструкциями
                var instructionSheet = package.Workbook.Worksheets.Add("Инструкция");
                instructionSheet.Cells[1, 1].Value = "Инструкция по заполнению вопросов на орфоэпию";
                instructionSheet.Cells[1, 1].Style.Font.Bold = true;
                instructionSheet.Cells[1, 1].Style.Font.Size = 14;

                instructionSheet.Cells[3, 1].Value = "Формат заполнения:";
                instructionSheet.Cells[3, 1].Style.Font.Bold = true;
                instructionSheet.Cells[4, 1].Value = "1. Слово* - слово без ударения (например: договор)";
                instructionSheet.Cells[5, 1].Value = "2. Позиция ударения* - номер слога с ударением, начиная с 1 (например: 3)";
                instructionSheet.Cells[6, 1].Value = "3. Слово с ударением* - слово с символом ударения после ударной гласной (например: договóр)";
                instructionSheet.Cells[7, 1].Value = "4. Подсказка - объяснение правила ударения (необязательно)";

                instructionSheet.Cells[9, 1].Value = "Символ ударения:";
                instructionSheet.Cells[9, 1].Style.Font.Bold = true;
                instructionSheet.Cells[10, 1].Value = "́ (скопируйте этот символ и вставляйте после ударной гласной)";
                instructionSheet.Cells[10, 1].Style.Font.Size = 16;

                instructionSheet.Cells[12, 1].Value = "Как определить позицию ударения:";
                instructionSheet.Cells[12, 1].Style.Font.Bold = true;
                instructionSheet.Cells[13, 1].Value = "• Слог - это гласная буква с окружающими согласными";
                instructionSheet.Cells[14, 1].Value = "• Считайте слоги по гласным буквам (а, е, ё, и, о, у, ы, э, ю, я)";
                instructionSheet.Cells[15, 1].Value = "• Например: до-го-вор = 3 слога, ударение на 3-й слог";
                instructionSheet.Cells[16, 1].Value = "• Например: зво-нит = 2 слога, ударение на 2-й слог";

                instructionSheet.Cells[18, 1].Value = "Важно:";
                instructionSheet.Cells[18, 1].Style.Font.Bold = true;
                instructionSheet.Cells[19, 1].Value = "• Не удаляйте строку с заголовками";
                instructionSheet.Cells[20, 1].Value = "• Заполняйте данные начиная со 2-й строки";
                instructionSheet.Cells[21, 1].Value = "• Позиция ударения должна быть числом от 1 до 20";
                instructionSheet.Cells[22, 1].Value = "• Обязательно используйте символ ударения ́ в третьем столбце";
                instructionSheet.Cells[23, 1].Value = "• Поля отмеченные * обязательны для заполнения";

                instructionSheet.Cells.AutoFitColumns();

                var templateBytes = await package.GetAsByteArrayAsync();
                _logger.LogInformation("Шаблон импорта вопросов орфоэпии успешно создан. Размер: {Size} байт", templateBytes.Length);
                return templateBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации шаблона импорта вопросов орфоэпии");
                throw;
            }
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

        private void ValidateQuestion(ImportOrthoeopyQuestionRow question)
        {
            question.IsValid = true;

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(question.Word))
            {
                question.Errors.Add("Слово обязательно для заполнения");
                question.IsValid = false;
            }

            if (question.StressPosition < 1 || question.StressPosition > 20)
            {
                question.Errors.Add("Позиция ударения должна быть от 1 до 20");
                question.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(question.WordWithStress))
            {
                question.Errors.Add("Слово с ударением обязательно для заполнения");
                question.IsValid = false;
            }

            // Проверка наличия символа ударения
            if (!string.IsNullOrWhiteSpace(question.WordWithStress))
            {
                if (!question.WordWithStress.Contains("́"))
                {
                    question.Errors.Add("Слово с ударением должно содержать символ ударения ́");
                    question.IsValid = false;
                }
            }

            // Проверка длины слова
            if (!string.IsNullOrWhiteSpace(question.Word) && question.Word.Length > 200)
            {
                question.Errors.Add("Слово слишком длинное (максимум 200 символов)");
                question.IsValid = false;
            }

            // Проверка соответствия количества слогов и позиции ударения
            if (!string.IsNullOrWhiteSpace(question.Word) && question.StressPosition > 0)
            {
                var vowels = new[] { 'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я',
                                    'А', 'Е', 'Ё', 'И', 'О', 'У', 'Ы', 'Э', 'Ю', 'Я' };
                var syllableCount = question.Word.Count(c => vowels.Contains(c));

                if (question.StressPosition > syllableCount)
                {
                    question.Errors.Add($"Позиция ударения ({question.StressPosition}) больше количества слогов ({syllableCount}) в слове");
                    question.IsValid = false;
                }
            }

            // Проверка длины подсказки
            if (!string.IsNullOrWhiteSpace(question.Hint) && question.Hint.Length > 1000)
            {
                question.Errors.Add("Подсказка слишком длинная (максимум 1000 символов)");
                question.IsValid = false;
            }

            // Проверка длины слова с ударением
            if (!string.IsNullOrWhiteSpace(question.WordWithStress) && question.WordWithStress.Length > 200)
            {
                question.Errors.Add("Слово с ударением слишком длинное (максимум 200 символов)");
                question.IsValid = false;
            }

            // Если есть ошибки, устанавливаем IsValid = false
            if (question.Errors.Any())
            {
                question.IsValid = false;
                _logger.LogWarning("Вопрос орфоэпии в строке {RowNumber} невалиден. Слово: {Word}, Ошибки: {@Errors}",
                    question.RowNumber, question.Word, question.Errors);
            }
        }
    }
}
