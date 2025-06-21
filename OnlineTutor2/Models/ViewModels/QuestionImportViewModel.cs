using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class QuestionImportViewModel
    {
        public int SpellingTestId { get; set; }

        [Required(ErrorMessage = "Выберите файл для импорта")]
        [Display(Name = "Excel файл с вопросами")]
        public IFormFile ExcelFile { get; set; }

        [Display(Name = "Баллы за каждый правильный ответ")]
        [Range(1, 10, ErrorMessage = "Баллы должны быть от 1 до 10")]
        public int PointsPerQuestion { get; set; } = 1;
    }

    public class ImportQuestionRow
    {
        public int RowNumber { get; set; }
        public string? WordWithGap { get; set; }
        public string? CorrectLetter { get; set; }
        public string? FullWord { get; set; }
        public string? Hint { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsValid => !Errors.Any();
    }
}
