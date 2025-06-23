using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;
using OnlineTutor2.Services;
using System.Text.Json;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class StudentManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStudentImportService _importService;

        // ОДИН конструктор со всеми зависимостями
        public StudentManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStudentImportService importService)
        {
            _context = context;
            _userManager = userManager;
            _importService = importService;
        }

        // GET: StudentManagement
        public async Task<IActionResult> Index(string? searchString, int? classFilter, string? sortOrder)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем классы текущего учителя для фильтра
            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id)
                .ToListAsync();

            ViewBag.Classes = new SelectList(teacherClasses, "Id", "Name");
            ViewBag.CurrentFilter = searchString;
            ViewBag.ClassFilter = classFilter;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

            // Получаем всех студентов
            var students = _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .AsQueryable();

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.User.FirstName.Contains(searchString) ||
                                             s.User.LastName.Contains(searchString) ||
                                             s.User.Email.Contains(searchString) ||
                                             (s.School != null && s.School.Contains(searchString)));
            }

            // Фильтрация по классу
            if (classFilter.HasValue)
            {
                students = students.Where(s => s.ClassId == classFilter.Value);
            }
            else
            {
                // Показываем только студентов, которые либо в классах текущего учителя, либо без класса
                var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
                students = students.Where(s => s.ClassId == null || teacherClassIds.Contains(s.ClassId.Value));
            }

            // Сортировка
            switch (sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.User.LastName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.EnrollmentDate);
                    break;
                case "date_desc":
                    students = students.OrderByDescending(s => s.EnrollmentDate);
                    break;
                default:
                    students = students.OrderBy(s => s.User.LastName);
                    break;
            }

            return View(await students.ToListAsync());
        }

        // GET: StudentManagement/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                    .ThenInclude(c => c.Teacher)
                .Include(s => s.TestResults)
                    .ThenInclude(tr => tr.Test)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Assignment)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            // Проверяем, имеет ли учитель доступ к этому студенту
            var currentUser = await _userManager.GetUserAsync(User);
            if (student.Class != null && student.Class.TeacherId != currentUser.Id)
            {
                return Forbid();
            }

            // Добавляем классы для модального окна
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");

            return View(student);
        }

        // GET: StudentManagement/Create
        public async Task<IActionResult> Create()
        {
            await LoadClassesForCreate();
            return View();
        }

        // POST: StudentManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Проверяем, существует ли пользователь с таким email
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Пользователь с таким email уже существует.");
                    await LoadClassesForCreate();
                    return View(model);
                }

                // Создаем пользователя
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Добавляем роль студента
                    await _userManager.AddToRoleAsync(user, ApplicationRoles.Student);

                    // Создаем профиль студента
                    var student = new Student
                    {
                        UserId = user.Id,
                        School = model.School,
                        Grade = model.Grade,
                        ClassId = model.ClassId,
                        StudentNumber = await GenerateStudentNumber()
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Ученик {user.FullName} успешно создан!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            await LoadClassesForCreate();
            return View(model);
        }

        // GET: StudentManagement/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            // Проверяем доступ
            var currentUser = await _userManager.GetUserAsync(User);
            if (student.Class != null && student.Class.TeacherId != currentUser.Id)
            {
                return Forbid();
            }

            var model = new EditStudentViewModel
            {
                Id = student.Id,
                FirstName = student.User.FirstName,
                LastName = student.User.LastName,
                Email = student.User.Email,
                PhoneNumber = student.User.PhoneNumber,
                DateOfBirth = student.User.DateOfBirth,
                School = student.School,
                Grade = student.Grade,
                ClassId = student.ClassId,
                StudentNumber = student.StudentNumber
            };

            await LoadClassesForEdit();
            return View(model);
        }

        // POST: StudentManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditStudentViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var student = await _context.Students
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (student == null) return NotFound();

                    // Обновляем данные пользователя
                    student.User.FirstName = model.FirstName;
                    student.User.LastName = model.LastName;
                    student.User.PhoneNumber = model.PhoneNumber;
                    student.User.DateOfBirth = model.DateOfBirth;

                    // Проверяем изменение email
                    if (student.User.Email != model.Email)
                    {
                        var existingUser = await _userManager.FindByEmailAsync(model.Email);
                        if (existingUser != null && existingUser.Id != student.User.Id)
                        {
                            ModelState.AddModelError("Email", "Пользователь с таким email уже существует.");
                            await LoadClassesForEdit();
                            return View(model);
                        }
                        student.User.Email = model.Email;
                        student.User.UserName = model.Email;
                    }

                    // Обновляем данные студента
                    student.School = model.School;
                    student.Grade = model.Grade;
                    student.ClassId = model.ClassId;
                    student.StudentNumber = model.StudentNumber;

                    await _userManager.UpdateAsync(student.User);
                    _context.Update(student);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Данные ученика {student.User.FullName} успешно обновлены!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            await LoadClassesForEdit();
            return View(model);
        }

        // GET: StudentManagement/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Include(s => s.TestResults)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // POST: StudentManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            var userName = student.User.FullName;

            // Удаляем связанные данные
            var testResults = _context.TestResults.Where(tr => tr.StudentId == id);
            _context.TestResults.RemoveRange(testResults);

            var grades = _context.Grades.Where(g => g.StudentId == id);
            _context.Grades.RemoveRange(grades);

            // Удаляем студента
            _context.Students.Remove(student);

            // Удаляем пользователя
            await _userManager.DeleteAsync(student.User);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ученик {userName} успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // POST: StudentManagement/AssignToClass
        [HttpPost]
        public async Task<IActionResult> AssignToClass(int studentId, int? classId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Если назначаем в класс, проверяем, что класс принадлежит текущему учителю
            if (classId.HasValue)
            {
                var @class = await _context.Classes.FindAsync(classId.Value);
                if (@class == null || @class.TeacherId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            student.ClassId = classId;
            await _context.SaveChangesAsync();

            var message = classId.HasValue
                ? $"Ученик назначен в класс успешно!"
                : "Ученик исключен из класса!";

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // Методы для работы с существующими учениками
        public async Task<IActionResult> AddExisting(string? searchString)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем классы текущего учителя
            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .ToListAsync();

            ViewBag.Classes = new SelectList(teacherClasses, "Id", "Name");

            // Получаем ID учеников, которые уже в классах этого учителя
            var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
            var studentsInTeacherClasses = await _context.Students
                .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                .Select(s => s.UserId)
                .ToListAsync();

            // Получаем всех пользователей с ролью Student
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == ApplicationRoles.Student);
            if (studentRole == null)
            {
                return View(new List<ApplicationUser>());
            }

            var availableStudents = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRole.Id))
                .Where(u => !studentsInTeacherClasses.Contains(u.Id))
                .Include(u => u.StudentProfile)
                .ToListAsync();

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                availableStudents = availableStudents.Where(u =>
                    u.FirstName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    (u.StudentProfile != null && u.StudentProfile.School != null &&
                     u.StudentProfile.School.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            ViewBag.SearchString = searchString;
            return View(availableStudents.OrderBy(u => u.LastName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingToClass(string userId, int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Проверяем, что класс принадлежит текущему учителю
            var @class = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == currentUser.Id);

            if (@class == null)
            {
                TempData["ErrorMessage"] = "Класс не найден или у вас нет прав для его изменения.";
                return RedirectToAction(nameof(AddExisting));
            }

            // Проверяем, что пользователь существует и является студентом
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !await _userManager.IsInRoleAsync(user, ApplicationRoles.Student))
            {
                TempData["ErrorMessage"] = "Пользователь не найден или не является учеником.";
                return RedirectToAction(nameof(AddExisting));
            }

            // Получаем или создаем профиль студента
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
            {
                // Создаем профиль студента, если его нет
                student = new Student
                {
                    UserId = userId,
                    StudentNumber = await GenerateStudentNumber(),
                    EnrollmentDate = DateTime.Now
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            // Назначаем студента в класс
            student.ClassId = classId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ученик {user.FullName} успешно добавлен в класс \"{@class.Name}\"!";
            return RedirectToAction(nameof(Index));
        }

        // Методы импорта
        public async Task<IActionResult> Import()
        {
            await LoadClassesForImport();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(ImportStudentsViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Проверка файла
                    if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                    {
                        ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                        await LoadClassesForImport();
                        return View(model);
                    }

                    var allowedExtensions = new[] { ".xlsx", ".xls" };
                    var fileExtension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ExcelFile", "Поддерживаются только файлы Excel (.xlsx, .xls)");
                        await LoadClassesForImport();
                        return View(model);
                    }

                    // Парсинг файла
                    var students = await _importService.ParseExcelFileAsync(
                        model.ExcelFile,
                        model.AutoGeneratePasswords,
                        model.DefaultPassword);

                    if (!students.Any())
                    {
                        TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                        await LoadClassesForImport();
                        return View(model);
                    }

                    // Сохраняем данные в TempData для следующего шага
                    TempData["ImportData"] = JsonSerializer.Serialize(new
                    {
                        Students = students,
                        ClassId = model.ClassId
                    });

                    return RedirectToAction(nameof(ImportPreview));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при обработке файла: {ex.Message}");
                }
            }

            await LoadClassesForImport();
            return View(model);
        }

        public async Task<IActionResult> ImportPreview()
        {
            var importDataJson = TempData["ImportData"] as string;
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["ErrorMessage"] = "Данные импорта не найдены. Попробуйте еще раз.";
                return RedirectToAction(nameof(Import));
            }

            try
            {
                var importData = JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var studentsArray = importData.GetProperty("Students");
                var students = new List<ImportStudentRow>();

                foreach (var studentElement in studentsArray.EnumerateArray())
                {
                    var student = JsonSerializer.Deserialize<ImportStudentRow>(studentElement.GetRawText());
                    students.Add(student);
                }

                TempData["ImportData"] = importDataJson;
                return View(students);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка обработки данных: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportConfirm()
        {
            var importDataJson = TempData["ImportData"] as string;
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["ErrorMessage"] = "Данные импорта не найдены";
                return RedirectToAction(nameof(Import));
            }

            try
            {
                var importData = JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var classId = importData.GetProperty("ClassId").ValueKind != JsonValueKind.Null
                    ? importData.GetProperty("ClassId").GetInt32()
                    : (int?)null;

                var result = new ImportResultViewModel();
                var currentUser = await _userManager.GetUserAsync(User);

                // Проверяем права на класс
                if (classId.HasValue)
                {
                    var @class = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == classId.Value && c.TeacherId == currentUser.Id);
                    if (@class == null)
                    {
                        TempData["ErrorMessage"] = "У вас нет прав для назначения в указанный класс";
                        return RedirectToAction(nameof(Import));
                    }
                }

                // Обрабатываем каждого студента
                var studentsArray = importData.GetProperty("Students");
                foreach (var studentElement in studentsArray.EnumerateArray())
                {
                    var student = JsonSerializer.Deserialize<ImportStudentRow>(studentElement.GetRawText());
                    result.TotalRows++;

                    try
                    {
                        // Проверяем, существует ли пользователь
                        var existingUser = await _userManager.FindByEmailAsync(student.Email);
                        if (existingUser != null)
                        {
                            student.Errors.Add($"Пользователь с email {student.Email} уже существует");
                            result.FailedRows.Add(student);
                            result.FailedImports++;
                            continue;
                        }

                        // Создаем пользователя
                        var user = new ApplicationUser
                        {
                            UserName = student.Email,
                            Email = student.Email,
                            FirstName = student.FirstName,
                            LastName = student.LastName,
                            DateOfBirth = student.DateOfBirth.Value,
                            PhoneNumber = student.PhoneNumber
                        };

                        var createResult = await _userManager.CreateAsync(user, student.Password);
                        if (createResult.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, ApplicationRoles.Student);

                            // Создаем профиль студента
                            var studentProfile = new Student
                            {
                                UserId = user.Id,
                                School = student.School,
                                Grade = student.Grade,
                                ClassId = classId,
                                StudentNumber = await GenerateStudentNumber()
                            };

                            _context.Students.Add(studentProfile);
                            await _context.SaveChangesAsync();

                            result.SuccessfulRows.Add(student);
                            result.SuccessfulImports++;
                        }
                        else
                        {
                            foreach (var error in createResult.Errors)
                            {
                                student.Errors.Add(error.Description);
                            }
                            result.FailedRows.Add(student);
                            result.FailedImports++;
                        }
                    }
                    catch (Exception ex)
                    {
                        student.Errors.Add($"Ошибка создания: {ex.Message}");
                        result.FailedRows.Add(student);
                        result.FailedImports++;
                    }
                }

                TempData["SuccessMessage"] = $"Импорт завершен! Успешно: {result.SuccessfulImports}, Ошибок: {result.FailedImports}";
                return View("ImportResult", result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при импорте: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                var templateBytes = await _importService.GenerateTemplateAsync();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Шаблон_импорта_учеников.xlsx");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка генерации шаблона: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        // Вспомогательные методы
        private async Task LoadClassesForCreate()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private async Task LoadClassesForEdit()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private async Task LoadClassesForImport()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private async Task<string> GenerateStudentNumber()
        {
            var currentYear = DateTime.Now.Year;
            var lastStudent = await _context.Students
                .Where(s => s.StudentNumber != null && s.StudentNumber.StartsWith(currentYear.ToString()))
                .OrderByDescending(s => s.StudentNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastStudent != null && lastStudent.StudentNumber.Length >= 8)
            {
                if (int.TryParse(lastStudent.StudentNumber.Substring(4), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{currentYear}{nextNumber:D4}";
        }
    }
}
