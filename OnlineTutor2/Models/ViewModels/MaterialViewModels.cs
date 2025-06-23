using System.ComponentModel.DataAnnotations;
using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class CreateMaterialViewModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        [Display(Name = "Название материала")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Выберите файл")]
        [Display(Name = "Файл")]
        public IFormFile File { get; set; } = null!;

        [Display(Name = "Назначить классу")]
        public int? ClassId { get; set; }

        [Display(Name = "Активный")]
        public bool IsActive { get; set; } = true;
    }

    public class EditMaterialViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        [Display(Name = "Название материала")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Новый файл (оставьте пустым, чтобы не менять)")]
        public IFormFile? NewFile { get; set; }

        [Display(Name = "Назначить классу")]
        public int? ClassId { get; set; }

        [Display(Name = "Активный")]
        public bool IsActive { get; set; } = true;

        public string? CurrentFileName { get; set; }
    }

    public class MaterialFilterViewModel
    {
        public string? SearchString { get; set; }
        public int? ClassFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? SortOrder { get; set; }
    }

    public class MaterialStatisticsViewModel
    {
        public int TotalMaterials { get; set; }
        public long TotalSize { get; set; }
        public Dictionary<MaterialType, int> MaterialsByType { get; set; } = new();
        public Dictionary<string, int> MaterialsByClass { get; set; } = new();
        public List<Material> RecentMaterials { get; set; } = new();
        public List<Material> LargestMaterials { get; set; } = new();

        public string TotalSizeFormatted => FormatFileSize(TotalSize);

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
