using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;
using OnlineTutor2.Services;
using System.Text.Json;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class StudentManagementController : Controller
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IClassRepository _classRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStudentImportService _importService;
        private readonly ILogger<StudentManagementController> _logger;

        public StudentManagementController(
            IStudentRepository studentRepository,
            IClassRepository classRepository,
            UserManager<ApplicationUser> userManager,
            IStudentImportService importService,
            ILogger<StudentManagementController> logger)
        {
            _studentRepository = studentRepository;
            _classRepository = classRepository;
            _userManager = userManager;
            _importService = importService;
            _logger = logger;
        }

        // GET: StudentManagement
        public async Task<IActionResult> Index(
            string? searchString,
            int? classFilter,
            string? sortOrder,
            int? pageNumber,
            int? pageSize)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем классы текущего учителя для фильтра
            var teacherClasses = await _classRepository.GetByTeacherIdAsync(currentUser.Id);

            ViewBag.Classes = new SelectList(teacherClasses, "Id", "Name");
            ViewBag.CurrentFilter = searchString;
            ViewBag.ClassFilter = classFilter;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

            // Получаем ID классов учителя
            var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
            
            // Пагинация
            int currentPageSize = pageSize ?? 10;
            int currentPageNumber = pageNumber ?? 1;
            
            // Получаем студентов через репозиторий
            var (paginatedStudents, totalCount) = await _studentRepository.GetPaginatedAsync(
                teacherClassIds: teacherClassIds.Count > 0 ? teacherClassIds : null,
                searchString: searchString,
                classFilter: classFilter,
                sortOrder: sortOrder,
                pageNumber: currentPageNumber,
                pageSize: currentPageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);

            if (currentPageNumber < 1)
                currentPageNumber = 1;
            if (currentPageNumber > totalPages && totalPages > 0)
                currentPageNumber = totalPages;

            ViewBag.PageNumber = currentPageNumber;
            ViewBag.PageSize = currentPageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            _logger.LogInformation("Учитель {TeacherId} просмотрел список учеников. Страница: {PageNumber}, Размер: {PageSize}, Всего: {TotalCount}",
                currentUser.Id, currentPageNumber, currentPageSize, totalCount);

            return View(paginatedStudents);
        }

        // GET: StudentManagement/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // TODO: Полная миграция Details требует репозиториев для результатов тестов и оценок
            var student = await _studentRepository.GetWithUserAsync(id.Value);
            if (student == null) return NotFound();

            // Проверяем, имеет ли учитель доступ к этому студенту
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            
            if (student.ClassId.HasValue)
            {
                var @class = await _classRepository.GetByIdAsync(student.ClassId.Value);
                if (@class != null && @class.TeacherId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            // Добавляем классы для модального окна
            var classes = await _classRepository.GetByTeacherIdAsync(currentUser.Id);
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
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                // Проверяем, существует ли пользователь с таким email
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Учитель {TeacherId} попытался создать ученика с существующим email {Email}", currentUser.Id, model.Email);
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

                    await _studentRepository.CreateAsync(student);

                    _logger.LogInformation("Учитель {TeacherId} создал ученика {StudentId}: {StudentName}, Email: {Email}, ClassId: {ClassId}",
                        currentUser.Id, student.Id, user.FullName, model.Email, model.ClassId);

                    TempData["SuccessMessage"] = $"Ученик {user.FullName} успешно создан!";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Учитель {TeacherId} не смог создать ученика {Email}. Ошибки: {@Errors}",
                    currentUser.Id, model.Email, result.Errors);

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму создания ученика", currentUser.Id);
            }

            await LoadClassesForCreate();
            return View(model);
        }

        // GET: StudentManagement/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _studentRepository.GetWithUserAsync(id.Value);

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

            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                try
                {
                    var student = await _studentRepository.GetWithUserAsync(id);

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
                            _logger.LogWarning("Учитель {TeacherId} попытался изменить email ученика {StudentId} на существующий {Email}",
                                currentUser.Id, id, model.Email);
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
                    await _studentRepository.UpdateAsync(student);

                    _logger.LogInformation("Учитель {TeacherId} обновил ученика {StudentId}: {StudentName}, ClassId: {ClassId}",
                        currentUser.Id, id, student.User.FullName, model.ClassId);

                    TempData["SuccessMessage"] = $"Данные ученика {student.User.FullName} успешно обновлены!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении ученика {StudentId} учителем {TeacherId}", id, currentUser.Id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }
            else
            {
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму обновления ученика {StudentId}", currentUser.Id, id);
            }

            await LoadClassesForEdit();
            return View(model);
        }

        // GET: StudentManagement/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // TODO: Полная миграция требует репозиториев для результатов тестов
            var student = await _studentRepository.GetWithUserAsync(id.Value);

            if (student == null) return NotFound();

            return View(student);
        }

        // POST: StudentManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // TODO: Полная миграция Delete требует репозиториев для результатов тестов
            var student = await _studentRepository.GetWithUserAsync(id);
            if (student == null || student.User == null) return NotFound();

            var userName = student.User.FullName;
            var userId = student.UserId;

            // TODO: Удаление связанных данных требует репозиториев для результатов тестов и оценок
            // Пока удаляем только студента и пользователя
            // Связанные данные должны удаляться каскадно на уровне БД или через репозитории
            
            // Удаляем студента
            await _studentRepository.DeleteAsync(student.Id);

            // Удаляем пользователя
            await _userManager.DeleteAsync(student.User);

            _logger.LogInformation("Учитель {TeacherId} удалил ученика {StudentId}, UserId: {UserId}, Имя: {StudentName}",
                currentUser.Id, id, userId, userName);

            TempData["SuccessMessage"] = $"Ученик {userName} успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // POST: StudentManagement/AssignToClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToClass(int studentId, int? classId)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Если назначаем в класс, проверяем, что класс принадлежит текущему учителю
            if (classId.HasValue)
            {
                var @class = await _classRepository.GetByIdAsync(classId.Value);
                if (@class == null || @class.TeacherId != currentUser.Id)
                {
                    return Forbid();
                }
            }

            var oldClassId = student.ClassId;
            student.ClassId = classId;
            await _studentRepository.UpdateAsync(student);

            var message = classId.HasValue
                ? $"Ученик назначен в онлайн-класс успешно!"
                : "Ученик исключен из онлайн-класса!";

            _logger.LogInformation("Учитель {TeacherId} {Action} ученика {StudentId}. Старый класс: {OldClassId}, Новый класс: {NewClassId}",
               currentUser.Id, classId.HasValue ? "назначил в класс" : "исключил из класса", studentId, oldClassId, classId);

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // Методы для работы с существующими учениками
        public async Task<IActionResult> AddExisting(string? searchString)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем классы текущего учителя
            var teacherClasses = await _classRepository.GetByTeacherIdAsync(currentUser.Id);

            ViewBag.Classes = new SelectList(teacherClasses, "Id", "Name");

            // Получаем ID учеников, которые уже в классах этого учителя
            var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
            var userIds = await _studentRepository.GetUserIdsInClassesAsync(teacherClassIds);
            var studentsInTeacherClasses = new HashSet<string>(userIds);

            // Получаем доступных студентов через репозиторий
            var availableStudents = await _studentRepository.GetAvailableStudentsAsync(
                studentsInTeacherClasses,
                searchString);

            ViewBag.SearchString = searchString;
            return View(availableStudents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingToClass(string userId, int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Проверяем, что класс принадлежит текущему учителю
            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null || @class.TeacherId != currentUser.Id)

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
            var student = await _studentRepository.GetByUserIdAsync(userId);
            if (student == null)
            {
                // Создаем профиль студента, если его нет
                student = new Student
                {
                    UserId = userId,
                    StudentNumber = await GenerateStudentNumber(),
                    CreatedAt = DateTime.Now
                };
                await _studentRepository.CreateAsync(student);

                _logger.LogInformation("Учитель {TeacherId} создал профиль студента {StudentId} для пользователя {UserId}",
                   currentUser.Id, student.Id, userId);
            }

            // Назначаем студента в класс
            student.ClassId = classId;
            // Сохранение уже выполнено через репозиторий

            _logger.LogInformation("Учитель {TeacherId} добавил существующего ученика {StudentId} ({StudentName}) в класс {ClassId} ({ClassName})",
                currentUser.Id, student.Id, user.FullName, classId, @class.Name);

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
            var currentUser = await _userManager.GetUserAsync(User);

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
                        _logger.LogWarning("Учитель {TeacherId} попытался импортировать файл недопустимого формата {FileExtension}",
                            currentUser.Id, fileExtension);
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
                        _logger.LogWarning("Учитель {TeacherId} импортировал пустой файл", currentUser.Id);
                        TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                        await LoadClassesForImport();
                        return View(model);
                    }

                    _logger.LogInformation("Учитель {TeacherId} успешно распарсил {StudentsCount} учеников для импорта",
                        currentUser.Id, students.Count);

                    TempData["ImportData"] = JsonSerializer.Serialize(new
                    {
                        Students = students,
                        ClassId = model.ClassId
                    });

                    return RedirectToAction(nameof(ImportPreview));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка импорта учеников учителем {TeacherId}", currentUser.Id);
                    ModelState.AddModelError("", $"Ошибка при обработке файла: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму импорта учеников", currentUser.Id);
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
                _logger.LogError(ex, "Ошибка обработки предпросмотра импорта учеников");
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

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var importData = JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var classId = importData.GetProperty("ClassId").ValueKind != JsonValueKind.Null
                    ? importData.GetProperty("ClassId").GetInt32()
                    : (int?)null;

                var result = new ImportResultViewModel();

                // Проверяем права на класс
                if (classId.HasValue)
                {
                    var @class = await _classRepository.GetByIdAsync(classId.Value);
                    if (@class == null || @class.TeacherId != currentUser.Id)
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

                            await _studentRepository.CreateAsync(studentProfile);

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
                        _logger.LogError(ex, "Ошибка импорта конкретного ученика {Email} учителем {TeacherId}",
                            student.Email, currentUser.Id);
                        student.Errors.Add($"Ошибка создания: {ex.Message}");
                        result.FailedRows.Add(student);
                        result.FailedImports++;
                    }
                }

                _logger.LogInformation("Учитель {TeacherId} завершил импорт учеников. Успешно: {SuccessfulCount}, Ошибок: {FailedCount}, Всего: {TotalCount}",
                    currentUser.Id, result.SuccessfulImports, result.FailedImports, result.TotalRows);

                TempData["SuccessMessage"] = $"Импорт завершен! Успешно: {result.SuccessfulImports}, Ошибок: {result.FailedImports}";
                return View("ImportResult", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения импорта учеников учителем {TeacherId}", currentUser.Id);
                TempData["ErrorMessage"] = $"Ошибка при импорте: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        public async Task<IActionResult> DownloadTemplate()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var templateBytes = await _importService.GenerateTemplateAsync();

                _logger.LogInformation("Учитель {TeacherId} скачал шаблон импорта учеников", currentUser.Id);

                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Шаблон_импорта_учеников.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации шаблона импорта учеников для учителя {TeacherId}", currentUser.Id);
                TempData["ErrorMessage"] = $"Ошибка генерации шаблона: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        // Вспомогательные методы
        private async Task LoadClassesForCreate()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;
            var classes = await _classRepository.GetByTeacherIdAsync(currentUser.Id);
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private async Task LoadClassesForEdit()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;
            var classes = await _classRepository.GetByTeacherIdAsync(currentUser.Id);
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private async Task LoadClassesForImport()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;
            var classes = await _classRepository.GetByTeacherIdAsync(currentUser.Id);
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        private async Task<string> GenerateStudentNumber()
        {
            var currentYear = DateTime.Now.Year;
            // Получаем последний номер студента через репозиторий
            var lastStudentNumber = await _studentRepository.GetLastStudentNumberAsync(currentYear);

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastStudentNumber) && lastStudentNumber.Length >= 8)
            {
                var numberPart = lastStudentNumber.Substring(4);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{currentYear}{nextNumber:D4}";
        }
    }
}
