using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class MaterialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MaterialController> _logger;

        // Разрешенные типы файлов и их размеры (в байтах)
        private readonly Dictionary<string, long> _allowedFileTypes = new()
        {
            // Документы
            { ".pdf", 50 * 1024 * 1024 },      // 50 MB
            { ".doc", 25 * 1024 * 1024 },      // 25 MB
            { ".docx", 25 * 1024 * 1024 },     // 25 MB
            { ".txt", 5 * 1024 * 1024 },       // 5 MB
            { ".rtf", 10 * 1024 * 1024 },      // 10 MB
            
            // Презентации
            { ".ppt", 50 * 1024 * 1024 },      // 50 MB
            { ".pptx", 50 * 1024 * 1024 },     // 50 MB
            
            // Таблицы
            { ".xls", 25 * 1024 * 1024 },      // 25 MB
            { ".xlsx", 25 * 1024 * 1024 },     // 25 MB
            { ".csv", 5 * 1024 * 1024 },       // 5 MB
            
            // Изображения
            { ".jpg", 10 * 1024 * 1024 },      // 10 MB
            { ".jpeg", 10 * 1024 * 1024 },     // 10 MB
            { ".png", 10 * 1024 * 1024 },      // 10 MB
            { ".gif", 5 * 1024 * 1024 },       // 5 MB
            { ".bmp", 15 * 1024 * 1024 },      // 15 MB
            
            // Аудио
            { ".mp3", 25 * 1024 * 1024 },      // 25 MB
            { ".wav", 50 * 1024 * 1024 },      // 50 MB
            { ".m4a", 25 * 1024 * 1024 },      // 25 MB
            
            // Видео
            { ".mp4", 200 * 1024 * 1024 },     // 200 MB
            { ".avi", 200 * 1024 * 1024 },     // 200 MB
            { ".mov", 200 * 1024 * 1024 },     // 200 MB
            { ".wmv", 200 * 1024 * 1024 },     // 200 MB
        };

        public MaterialController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<MaterialController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // GET: Material
        public async Task<IActionResult> Index(string? searchString, int? classFilter, string? typeFilter, string? sortOrder)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            ViewBag.CurrentFilter = searchString;
            ViewBag.ClassFilter = classFilter;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.TitleSortParm = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.SizeSortParm = sortOrder == "Size" ? "size_desc" : "Size";

            // Получаем классы для фильтра
            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id)
                .ToListAsync();
            ViewBag.Classes = new SelectList(teacherClasses, "Id", "Name");

            // Получаем материалы текущего учителя
            var materials = _context.Materials
                .Include(m => m.Class)
                .Where(m => m.UploadedById == currentUser.Id)
                .AsQueryable();

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                materials = materials.Where(m => m.Title.Contains(searchString) ||
                                               (m.Description != null && m.Description.Contains(searchString)) ||
                                               (m.FileName != null && m.FileName.Contains(searchString)));
            }

            // Фильтрация по классу
            if (classFilter.HasValue)
            {
                materials = materials.Where(m => m.ClassId == classFilter.Value);
            }

            // Фильтрация по типу
            if (!string.IsNullOrEmpty(typeFilter))
            {
                if (Enum.TryParse<MaterialType>(typeFilter, out var materialType))
                {
                    materials = materials.Where(m => m.Type == materialType);
                }
            }

            // Сортировка
            switch (sortOrder)
            {
                case "title_desc":
                    materials = materials.OrderByDescending(m => m.Title);
                    break;
                case "Date":
                    materials = materials.OrderBy(m => m.UploadedAt);
                    break;
                case "date_desc":
                    materials = materials.OrderByDescending(m => m.UploadedAt);
                    break;
                case "Size":
                    materials = materials.OrderBy(m => m.FileSize);
                    break;
                case "size_desc":
                    materials = materials.OrderByDescending(m => m.FileSize);
                    break;
                default:
                    materials = materials.OrderBy(m => m.Title);
                    break;
            }

            var materialsList = await materials.ToListAsync();

            // Добавляем типы материалов для фильтра
            ViewBag.MaterialTypes = Enum.GetValues<MaterialType>()
                .Select(mt => new SelectListItem
                {
                    Value = mt.ToString(),
                    Text = GetMaterialTypeDisplayName(mt)
                })
                .ToList();

            return View(materialsList);
        }

        // GET: Material/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var material = await _context.Materials
                .Include(m => m.Class)
                .Include(m => m.UploadedBy)
                .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == currentUser.Id);

            if (material == null) return NotFound();

            return View(material);
        }

        // GET: Material/Create
        public async Task<IActionResult> Create()
        {
            await LoadClasses();
            return View();
        }

        // POST: Material/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaterialViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Валидация файла
                var validationResult = ValidateFile(model.File);
                if (!validationResult.IsValid)
                {
                    ModelState.AddModelError("File", validationResult.ErrorMessage);
                    await LoadClasses();
                    return View(model);
                }

                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);

                    // Сохраняем файл
                    var filePath = await SaveFileAsync(model.File);

                    var material = new Material
                    {
                        Title = model.Title,
                        Description = model.Description,
                        FilePath = filePath,
                        FileName = model.File.FileName,
                        FileSize = model.File.Length,
                        ContentType = model.File.ContentType,
                        Type = DetermineMaterialType(model.File.FileName),
                        ClassId = model.ClassId,
                        UploadedById = currentUser.Id,
                        IsActive = model.IsActive
                    };

                    _context.Materials.Add(material);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Material {Title} uploaded by user {UserId}",
                        material.Title, currentUser.Id);

                    TempData["SuccessMessage"] = $"Материал \"{material.Title}\" успешно загружен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading material");
                    ModelState.AddModelError("", "Произошла ошибка при загрузке файла. Попробуйте еще раз.");
                }
            }

            await LoadClasses();
            return View(model);
        }

        // GET: Material/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == currentUser.Id);

            if (material == null) return NotFound();

            var model = new EditMaterialViewModel
            {
                Id = material.Id,
                Title = material.Title,
                Description = material.Description,
                ClassId = material.ClassId,
                IsActive = material.IsActive,
                CurrentFileName = material.FileName
            };

            await LoadClasses();
            return View(model);
        }

        // POST: Material/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditMaterialViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var material = await _context.Materials
                        .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == currentUser.Id);

                    if (material == null) return NotFound();

                    // Обновляем основные поля
                    material.Title = model.Title;
                    material.Description = model.Description;
                    material.ClassId = model.ClassId;
                    material.IsActive = model.IsActive;

                    // Если загружен новый файл
                    if (model.NewFile != null)
                    {
                        var validationResult = ValidateFile(model.NewFile);
                        if (!validationResult.IsValid)
                        {
                            ModelState.AddModelError("NewFile", validationResult.ErrorMessage);
                            await LoadClasses();
                            return View(model);
                        }

                        // Удаляем старый файл
                        DeleteFile(material.FilePath);

                        // Сохраняем новый файл
                        var newFilePath = await SaveFileAsync(model.NewFile);

                        material.FilePath = newFilePath;
                        material.FileName = model.NewFile.FileName;
                        material.FileSize = model.NewFile.Length;
                        material.ContentType = model.NewFile.ContentType;
                        material.Type = DetermineMaterialType(model.NewFile.FileName);
                    }

                    _context.Update(material);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Материал \"{material.Title}\" успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating material {Id}", id);
                    ModelState.AddModelError("", "Произошла ошибка при обновлении материала.");
                }
            }

            await LoadClasses();
            return View(model);
        }

        // GET: Material/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var material = await _context.Materials
                .Include(m => m.Class)
                .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == currentUser.Id);

            if (material == null) return NotFound();

            return View(material);
        }

        // POST: Material/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == currentUser.Id);

            if (material == null) return NotFound();

            try
            {
                // Удаляем файл с диска
                DeleteFile(material.FilePath);

                // Удаляем запись из БД
                _context.Materials.Remove(material);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Material {Title} (ID: {Id}) deleted by user {UserId}",
                    material.Title, material.Id, currentUser.Id);

                TempData["SuccessMessage"] = $"Материал \"{material.Title}\" успешно удален!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material {Id}", id);
                TempData["ErrorMessage"] = "Произошла ошибка при удалении материала.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Material/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Проверяем права доступа (учитель может скачивать свои материалы или материалы своих классов)
            var material = await _context.Materials
                .Include(m => m.Class)
                .FirstOrDefaultAsync(m => m.Id == id &&
                    (m.UploadedById == currentUser.Id ||
                     (m.Class != null && m.Class.TeacherId == currentUser.Id)));

            if (material == null) return NotFound();

            var filePath = Path.Combine(_environment.WebRootPath, material.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Файл не найден на сервере.";
                return RedirectToAction(nameof(Index));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            _logger.LogInformation("Material {Title} (ID: {Id}) downloaded by user {UserId}",
                material.Title, material.Id, currentUser.Id);

            return File(fileBytes, material.ContentType ?? "application/octet-stream", material.FileName);
        }

        // POST: Material/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == currentUser.Id);

            if (material == null) return NotFound();

            material.IsActive = !material.IsActive;
            await _context.SaveChangesAsync();

            var status = material.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Материал \"{material.Title}\" {status}.";

            return RedirectToAction(nameof(Index));
        }

        #region Private Methods

        private async Task LoadClasses()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "Выберите файл для загрузки");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_allowedFileTypes.ContainsKey(extension))
            {
                var allowedExtensions = string.Join(", ", _allowedFileTypes.Keys);
                return (false, $"Неподдерживаемый тип файла. Разрешены: {allowedExtensions}");
            }

            var maxSize = _allowedFileTypes[extension];
            if (file.Length > maxSize)
            {
                var maxSizeMB = maxSize / (1024 * 1024);
                return (false, $"Размер файла превышает {maxSizeMB} МБ для данного типа");
            }

            return (true, string.Empty);
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "materials");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/materials/{uniqueFileName}";
        }

        private void DeleteFile(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete file {FilePath}", filePath);
            }
        }

        private MaterialType DetermineMaterialType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => MaterialType.Document,
                ".ppt" or ".pptx" => MaterialType.Presentation,
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => MaterialType.Image,
                ".mp3" or ".wav" or ".m4a" => MaterialType.Audio,
                ".mp4" or ".avi" or ".mov" or ".wmv" => MaterialType.Video,
                _ => MaterialType.Other
            };
        }

        private string GetMaterialTypeDisplayName(MaterialType type)
        {
            return type switch
            {
                MaterialType.Document => "Документы",
                MaterialType.Presentation => "Презентации",
                MaterialType.Image => "Изображения",
                MaterialType.Audio => "Аудио",
                MaterialType.Video => "Видео",
                MaterialType.Other => "Другое",
                _ => type.ToString()
            };
        }

        #endregion
    }
}
