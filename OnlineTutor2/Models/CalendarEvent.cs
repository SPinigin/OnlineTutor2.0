using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class CalendarEvent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        // Связь с учителем
        [Required]
        public string TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; }

        // Занятие может быть для класса
        public int? ClassId { get; set; }
        public Class Class { get; set; }

        // Или для отдельного ученика
        public int? StudentId { get; set; }
        public Student Student { get; set; }

        [StringLength(200)]
        public string? Location { get; set; } // Место (онлайн, кабинет и т.д.)

        [StringLength(50)]
        public string? Color { get; set; } // Цвет для календаря

        public bool IsRecurring { get; set; } // Повторяющееся событие

        [StringLength(100)]
        public string RecurrencePattern { get; set; } // Паттерн повторения

        public bool IsCompleted { get; set; } // Занятие завершено

        [StringLength(500)]
        public string? Notes { get; set; } // Заметки после занятия

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
