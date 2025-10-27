using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.ViewModels
{
    public class CreateCalendarEventViewModel
    {
        [Required(ErrorMessage = "Название занятия обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        [Display(Name = "Название занятия")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Дата и время начала обязательны")]
        [Display(Name = "Начало занятия")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "Дата и время окончания обязательны")]
        [Display(Name = "Окончание занятия")]
        public DateTime EndDateTime { get; set; }

        [Display(Name = "Класс")]
        public int? ClassId { get; set; }

        [Display(Name = "Ученик")]
        public int? StudentId { get; set; }

        [StringLength(200)]
        [Display(Name = "Место проведения")]
        public string Location { get; set; }

        [Display(Name = "Цвет")]
        public string Color { get; set; }

        [Display(Name = "Повторяющееся событие")]
        public bool IsRecurring { get; set; }

        [Display(Name = "Повторять")]
        public string RecurrencePattern { get; set; }
    }

    public class EditCalendarEventViewModel : CreateCalendarEventViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Занятие завершено")]
        public bool IsCompleted { get; set; }

        [StringLength(500)]
        [Display(Name = "Заметки")]
        public string Notes { get; set; }
    }

    public class CalendarEventDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string ClassName { get; set; }
        public string StudentName { get; set; }
        public string Location { get; set; }
        public string Color { get; set; }
        public bool IsCompleted { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
