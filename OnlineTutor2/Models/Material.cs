using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public string FilePath { get; set; }

        public string? FileName { get; set; }

        public long FileSize { get; set; }

        public string? ContentType { get; set; }

        public MaterialType Type { get; set; }

        public int ClassId { get; set; }

        public string UploadedById { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual Class Class { get; set; }
        public virtual ApplicationUser UploadedBy { get; set; }
    }

    public enum MaterialType
    {
        Document = 1,
        Video = 2,
        Audio = 3,
        Image = 4,
        Presentation = 5,
        Other = 6
    }
}
