using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.Services;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExportService _exportService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IExportService exportService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _exportService = exportService;
        }

        private string GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var stats = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                TotalTeachers = await _context.Teachers.CountAsync(),
                TotalClasses = await _context.Classes.CountAsync(),
                TotalSpellingTests = await _context.SpellingTests.CountAsync(),
                TotalRegularTests = await _context.RegularTests.CountAsync(),
                TotalPunctuationTests = await _context.PunctuationTests.CountAsync(),
                TotalOrthoeopyTests = await _context.OrthoeopyTests.CountAsync(),
                TotalTestResults = 
                    await _context.SpellingTestResults.CountAsync() + 
                    await _context.PunctuationTestResults.CountAsync() + 
                    await _context.RegularTestResults.CountAsync() + 
                    await _context.OrthoeopyTestResults.CountAsync(),
                PendingTeachers = await _context.Teachers.CountAsync(t => !t.IsApproved),

                RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                RecentTests = await _context.SpellingTests
                    .Include(st => st.Teacher)
                    .OrderByDescending(st => st.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            ViewBag.RecentLogs = await _auditLogService.GetLogsAsync(page: 1, pageSize: 5);

            return View(stats);
        }

        #region Users Management

        // GET: Admin/Users
        public async Task<IActionResult> Users(string? searchString, string? roleFilter, string? sortOrder)
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.EmailSortParm = sortOrder == "Email" ? "email_desc" : "Email";

            var users = _userManager.Users
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .AsQueryable();

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.FirstName.Contains(searchString) ||
                                       u.LastName.Contains(searchString) ||
                                       u.Email.Contains(searchString));
            }

            // Сортировка
            switch (sortOrder)
            {
                case "name_desc":
                    users = users.OrderByDescending(u => u.LastName);
                    break;
                case "Email":
                    users = users.OrderBy(u => u.Email);
                    break;
                case "email_desc":
                    users = users.OrderByDescending(u => u.Email);
                    break;
                case "Date":
                    users = users.OrderBy(u => u.CreatedAt);
                    break;
                case "date_desc":
                    users = users.OrderByDescending(u => u.CreatedAt);
                    break;
                default:
                    users = users.OrderBy(u => u.LastName);
                    break;
            }

            var usersList = await users.ToListAsync();
            var usersWithRoles = new List<AdminUserViewModel>();

            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userViewModel = new AdminUserViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                };

                // Фильтрация по роли
                if (string.IsNullOrEmpty(roleFilter) || roles.Contains(roleFilter))
                {
                    usersWithRoles.Add(userViewModel);
                }
            }

            ViewBag.Roles = ApplicationRoles.AllRoles;
            return View(usersWithRoles);
        }

        // GET: Admin/UserDetails/5
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.RegularTestResults)
                .Include(s => s.SpellingTestResults)
                .Include(s => s.PunctuationTestResults)
                .Include(s => s.OrthoeopyTestResults)
                .FirstOrDefaultAsync(s => s.UserId == id);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == id);

            var viewModel = new AdminUserDetailsViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Student = student,
                Teacher = teacher
            };

            return View(viewModel);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var adminId = _userManager.GetUserId(User);
            var adminName = User.Identity?.Name ?? "Unknown";
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующего пользователя {UserId}", adminId, id);
                return NotFound();
            }

            var userName = user.FullName;
            var userEmail = user.Email;

            // Удаляем связанные данные
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == id);
            if (student != null)
            {
                var regularResults = _context.RegularTestResults.Where(tr => tr.StudentId == student.Id);
                var spellingResults = _context.SpellingTestResults.Where(str => str.StudentId == student.Id);
                var punctuationResults = _context.PunctuationTestResults.Where(str => str.StudentId == student.Id);
                var orthoeopyResults = _context.OrthoeopyTestResults.Where(str => str.StudentId == student.Id);
                var grades = _context.Grades.Where(g => g.StudentId == student.Id);

                _context.RegularTestResults.RemoveRange(regularResults);
                _context.SpellingTestResults.RemoveRange(spellingResults);
                _context.PunctuationTestResults.RemoveRange(punctuationResults);
                _context.OrthoeopyTestResults.RemoveRange(orthoeopyResults);
                _context.Grades.RemoveRange(grades);
                _context.Students.Remove(student);

                _logger.LogInformation("Удалены связанные данные студента {StudentId}: результаты тестов, оценки", student.Id);
            }

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
                _logger.LogInformation("Удален профиль учителя {TeacherId}", teacher.Id);
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    AuditActions.UserDeleted,
                    AuditEntityTypes.User,
                    id,
                    $"Deleted user: {userName} ({userEmail})",
                    GetIpAddress()
                );
                _logger.LogInformation("Администратор {AdminId} успешно удалил пользователя {UserId}, Email: {Email}", adminId, id, user.Email);
                TempData["SuccessMessage"] = $"Пользователь {user.FullName} успешно удален!";
            }
            else
            {
                _logger.LogError("Ошибка удаления пользователя {UserId} администратором {AdminId}. Errors: {@Errors}", id, adminId, result.Errors);
                TempData["ErrorMessage"] = "Ошибка при удалении пользователя.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/ToggleUserStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var adminId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался изменить статус несуществующего пользователя {UserId}", adminId, id);
                return NotFound();
            }

            var oldStatus = user.IsActive;
            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _auditLogService.LogActionAsync(
                    adminId!,
                    User.Identity?.Name ?? "Unknown",
                    user.IsActive ? AuditActions.UserActivated : AuditActions.UserDeactivated,
                    AuditEntityTypes.User,
                    id,
                    $"{(user.IsActive ? "Activated" : "Deactivated")} user: {user.FullName}",
                    GetIpAddress()
);
                var status = user.IsActive ? "активирован" : "заблокирован";
                _logger.LogInformation("Администратор {AdminId} изменил статус пользователя {UserId} с {OldStatus} на {NewStatus}", adminId, id, oldStatus, user.IsActive);
                TempData["InfoMessage"] = $"Пользователь {user.FullName} {status}.";
            }
            else
            {
                _logger.LogError("Ошибка изменения статуса пользователя {UserId} администратором {AdminId}. Errors: {@Errors}", id, adminId, result.Errors);
            }

                return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Teachers Management

        // GET: Admin/Teachers
        public async Task<IActionResult> Teachers()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.User.TeacherClasses)
                .OrderBy(t => t.User.LastName)
                .ToListAsync();

            return View(teachers);
        }

        // POST: Admin/ApproveTeacher/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTeacher(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался одобрить несуществующего учителя {TeacherId}", adminId, id);
                return NotFound();
            }

            teacher.IsApproved = true;
            var result = await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                adminId!,
                User.Identity?.Name ?? "Unknown",
                AuditActions.TeacherApproved,
                AuditEntityTypes.Teacher,
                id.ToString(),
                $"Approved teacher: {teacher.User.FullName}",
                GetIpAddress()
            );

            _logger.LogInformation("Администратор {AdminId} одобрил учителя {TeacherId}, UserId: {UserId}, Email: {Email}", adminId, id, teacher.UserId, teacher.User.Email);
            TempData["SuccessMessage"] = $"Учитель {teacher.User.FullName} одобрен!";

            return RedirectToAction(nameof(Teachers));
        }

        #endregion

        #region Classes Management

        // GET: Admin/Classes
        public async Task<IActionResult> Classes()
        {
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .Include(c => c.RegularTests)
                .Include(c => c.SpellingTests)
                .Include(c => c.PunctuationTests)
                .Include(c => c.OrthoeopyTests)
                .Include(c => c.Materials)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(classes);
        }

        // POST: Admin/DeleteClass/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var @class = await _context.Classes
                .Include(c => c.Students)
                .Include(c => c.RegularTests)
                .Include(c => c.SpellingTests)
                .Include(c => c.PunctuationTests)
                .Include(c => c.OrthoeopyTests)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (@class == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий класс {ClassId}", adminId, id);
                return NotFound();
            }

            var className = @class.Name;
            var studentsCount = @class.Students.Count;
            var testsCount = @class.RegularTests.Count + @class.SpellingTests.Count + @class.PunctuationTests.Count + @class.OrthoeopyTests.Count;

            // Убираем студентов из класса
            foreach (var student in @class.Students)
            {
                student.ClassId = null;
            }

            // Убираем привязку тестов к классу
            foreach (var test in @class.RegularTests)
            {
                test.ClassId = null;
            }

            foreach (var test in @class.SpellingTests)
            {
                test.ClassId = null;
            }

            foreach (var test in @class.PunctuationTests)
            {
                test.ClassId = null;
            }

            foreach (var test in @class.OrthoeopyTests)
            {
                test.ClassId = null;
            }

            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                adminId!,
                User.Identity?.Name ?? "Unknown",
                AuditActions.ClassDeleted,
                AuditEntityTypes.Class,
                id.ToString(),
                $"Deleted class: {className} (Students: {studentsCount}, Tests: {testsCount})",
                GetIpAddress()
            );

            _logger.LogInformation("Администратор {AdminId} удалил класс {ClassId}, Название: {ClassName}, Студентов: {StudentsCount}, Тестов: {TestsCount}", adminId, id, className, studentsCount, testsCount);

            TempData["SuccessMessage"] = $"Класс {@class.Name} удален!";
            return RedirectToAction(nameof(Classes));
        }

        #endregion

        #region Tests Management

        // GET: Admin/Tests
        public async Task<IActionResult> Tests()
        {
            var adminId = _userManager.GetUserId(User);

            var spellingTests = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                .Include(st => st.SpellingQuestions)
                .Include(st => st.SpellingTestResults)
                .Select(st => new AdminTestViewModel
                {
                    Id = st.Id,
                    Title = st.Title,
                    Type = "Орфография",
                    TeacherName = st.Teacher.FullName,
                    ClassName = st.Class != null ? st.Class.Name : "Все ученики",
                    QuestionsCount = st.SpellingQuestions.Count,
                    ResultsCount = st.SpellingTestResults.Count,
                    CreatedAt = st.CreatedAt,
                    IsActive = st.IsActive,
                    ControllerName = "SpellingTest"
                })
                .ToListAsync();

            var regularTests = await _context.RegularTests
                .Include(t => t.Teacher)
                .Include(t => t.Class)
                .Include(t => t.RegularQuestions)
                .Include(t => t.RegularTestResults)
                .Select(t => new AdminTestViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = "Обычный",
                    TeacherName = t.Teacher.FullName,
                    ClassName = t.Class != null ? t.Class.Name : "Все ученики",
                    QuestionsCount = t.RegularQuestions.Count,
                    ResultsCount = t.RegularTestResults.Count,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive,
                    ControllerName = "RegularTest"
                })
                .ToListAsync();

            var allTests = spellingTests.Concat(regularTests)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return View(allTests);
        }

        // POST: Admin/DeleteSpellingTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpellingTest(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var test = await _context.SpellingTests
                .Include(st => st.SpellingTestResults)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (test == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий тест орфографии {TestId}", adminId, id);
                return NotFound();
            }

            var testTitle = test.Title;
            var resultsCount = test.SpellingTestResults.Count;

            _context.SpellingTests.Remove(test);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Администратор {AdminId} удалил тест орфографии {TestId}, Название: {TestTitle}, Результатов: {ResultsCount}", adminId, id, testTitle, resultsCount);

            TempData["SuccessMessage"] = $"Тест \"{test.Title}\" удален!";
            return RedirectToAction(nameof(Tests));
        }

        // POST: Admin/DeleteRegularTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegularTest(int id)
        {
            var test = await _context.RegularTests
                .Include(t => t.RegularTestResults)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            _context.RegularTests.Remove(test);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Тест \"{test.Title}\" удален!";
            return RedirectToAction(nameof(Tests));
        }

        #endregion

        #region Test Results Management

        // GET: Admin/TestResults
        public async Task<IActionResult> TestResults()
        {
            var spellingResults = await _context.SpellingTestResults
                .Include(str => str.SpellingTest)
                .Include(str => str.Student)
                    .ThenInclude(s => s.User)
                .Select(str => new AdminTestResultViewModel
                {
                    Id = str.Id,
                    TestTitle = str.SpellingTest.Title,
                    TestType = "Орфография",
                    StudentName = str.Student.User.FullName,
                    Score = str.Score,
                    MaxScore = str.MaxScore,
                    Percentage = str.Percentage,
                    StartedAt = str.StartedAt,
                    CompletedAt = str.CompletedAt,
                    IsCompleted = str.IsCompleted,
                    ResultType = "Spelling"
                })
                .ToListAsync();

            var regularResults = await _context.RegularTestResults
                .Include(tr => tr.RegularTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Select(tr => new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = tr.RegularTest.Title,
                    TestType = "Классический",
                    StudentName = tr.Student.User.FullName,
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    IsCompleted = tr.IsCompleted,
                    ResultType = "Regular"
                })
                .ToListAsync();

            var punctuationResults = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Select(tr => new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = tr.PunctuationTest.Title,
                    TestType = "Пунктуация",
                    StudentName = tr.Student.User.FullName,
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    IsCompleted = tr.IsCompleted,
                    ResultType = "Punctuation"
                })
                .ToListAsync();

            var orthoeopyResults = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Select(tr => new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = tr.OrthoeopyTest.Title,
                    TestType = "Орфоэпия",
                    StudentName = tr.Student.User.FullName,
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    IsCompleted = tr.IsCompleted,
                    ResultType = "Orthoeopy"
                })
                .ToListAsync();

            var allResults = spellingResults
                .Concat(regularResults)
                .Concat(punctuationResults)
                .Concat(orthoeopyResults)
                .OrderByDescending(r => r.StartedAt)
                .ToList();

            return View(allResults);
        }

        // POST: Admin/DeleteSpellingResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpellingResult(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var result = await _context.SpellingTestResults.FindAsync(id);
            if (result == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий результат теста орфографии {ResultId}", adminId, id);
                return NotFound();
            }

            _context.SpellingTestResults.Remove(result);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Администратор {AdminId} удалил результат теста орфографии {ResultId}, TestId: {TestId}, StudentId: {StudentId}", adminId, id, result.SpellingTestId, result.StudentId);

            TempData["SuccessMessage"] = "Результат теста удален!";
            return RedirectToAction(nameof(TestResults));
        }

        // POST: Admin/DeleteRegularResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegularResult(int id)
        {
            var result = await _context.RegularTestResults.FindAsync(id);
            if (result == null) return NotFound();

            _context.RegularTestResults.Remove(result);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Результат теста удален!";
            return RedirectToAction(nameof(TestResults));
        }

        // POST: Admin/ClearAllResults
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllResults()
        {
            var adminId = _userManager.GetUserId(User);
            var adminName = User.Identity?.Name ?? "Unknown";
            _logger.LogWarning("Администратор {AdminId} начал очистку всех результатов тестов", adminId);

            var spellingResults = await _context.SpellingTestResults.ToListAsync();
            var regularResults = await _context.RegularTestResults.ToListAsync();
            var punctuationResults = await _context.PunctuationTestResults.ToListAsync();
            var orthoeopyResults = await _context.OrthoeopyTestResults.ToListAsync();

            var totalCount = spellingResults.Count + regularResults.Count + punctuationResults.Count + orthoeopyResults.Count;

            _context.SpellingTestResults.RemoveRange(spellingResults);
            _context.RegularTestResults.RemoveRange(regularResults);
            _context.PunctuationTestResults.RemoveRange(punctuationResults);
            _context.OrthoeopyTestResults.RemoveRange(orthoeopyResults);

            var result = await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                adminId!,
                User.Identity?.Name ?? "Unknown",
                AuditActions.AllResultsCleared,
                AuditEntityTypes.System,
                null,
                $"Cleared all test results (Total: {totalCount})",
                GetIpAddress()
            );

            _logger.LogWarning("Администратор {AdminId} удалил все результаты тестов. Всего удалено: {TotalCount} (Орфография: {SpellingCount}, " +
            "Классические: {RegularCount}, Пунктуация: {PunctuationCount}, Орфоэпия: {OrthoeopyCount})", adminId, totalCount, spellingResults.Count,
            regularResults.Count, punctuationResults.Count, orthoeopyResults.Count);
            TempData["SuccessMessage"] = $"Удалено {totalCount} результатов тестов!";

            return RedirectToAction(nameof(TestResults));
        }

        #endregion

        #region System Management

        // GET: Admin/SystemInfo
        public async Task<IActionResult> SystemInfo()
        {
            var systemInfo = new AdminSystemInfoViewModel
            {
                DatabaseInfo = new DatabaseInfoViewModel
                {
                    TotalUsers = await _userManager.Users.CountAsync(),
                    TotalStudents = await _context.Students.CountAsync(),
                    TotalTeachers = await _context.Teachers.CountAsync(),
                    TotalClasses = await _context.Classes.CountAsync(),
                    TotalSpellingTests = await _context.SpellingTests.CountAsync(),
                    TotalRegularTests = await _context.RegularTests.CountAsync(),
                    TotalSpellingQuestions = await _context.SpellingQuestions.CountAsync(),
                    TotalRegularQuestions = await _context.RegularQuestions.CountAsync(),
                    TotalSpellingResults = await _context.SpellingTestResults.CountAsync(),
                    TotalRegularResults = await _context.RegularTestResults.CountAsync(),
                    TotalMaterials = await _context.Materials.CountAsync()
                }
            };

            return View(systemInfo);
        }

        #endregion

        #region Audit Logs Management

        // GET: Admin/AuditLogs
        public async Task<IActionResult> AuditLogs(
            DateTime? fromDate,
            DateTime? toDate,
            string? action,
            string? entityType,
            int page = 1)
        {
            const int pageSize = 50;

            var logs = await _auditLogService.GetLogsAsync(
                fromDate,
                toDate,
                null,
                action,
                entityType,
                page,
                pageSize
            );

            var totalCount = await _auditLogService.GetLogsCountAsync(
                fromDate,
                toDate,
                null,
                action,
                entityType
            );

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Action = action;
            ViewBag.EntityType = entityType;

            // Получаем уникальные действия и типы сущностей для фильтров
            ViewBag.Actions = await _context.AuditLogs
                .Select(al => al.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            ViewBag.EntityTypes = await _context.AuditLogs
                .Select(al => al.EntityType)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            return View(logs);
        }

        // POST: Admin/ClearOldLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOldLogs(int daysToKeep = 90)
        {
            var adminId = _userManager.GetUserId(User);
            var adminName = User.Identity?.Name ?? "Unknown";

            try
            {
                await _auditLogService.ClearOldLogsAsync(daysToKeep);

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Audit Logs Cleared",
                    AuditEntityTypes.System,
                    null,
                    $"Cleared audit logs older than {daysToKeep} days",
                    GetIpAddress()
                );

                TempData["SuccessMessage"] = $"Старые логи (старше {daysToKeep} дней) успешно удалены!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing old audit logs");
                TempData["ErrorMessage"] = "Ошибка при очистке старых логов.";
            }

            return RedirectToAction(nameof(AuditLogs));
        }

        #endregion

        #region Statistics

        // GET: Admin/Statistics
        public async Task<IActionResult> Statistics()
        {
            var stats = new AdminStatisticsViewModel();
            var now = DateTime.Now;
            var thirtyDaysAgo = now.AddDays(-30);

            // Регистрации пользователей за последние 30 дней
            var registrations = await _context.Users
                .Where(u => u.CreatedAt >= thirtyDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            stats.UserRegistrationsByDate = registrations.ToDictionary(x => x.Date, x => x.Count);

            // Заполняем пропущенные дни нулями
            for (var date = thirtyDaysAgo.Date; date <= now.Date; date = date.AddDays(1))
            {
                if (!stats.UserRegistrationsByDate.ContainsKey(date))
                {
                    stats.UserRegistrationsByDate[date] = 0;
                }
            }
            stats.UserRegistrationsByDate = stats.UserRegistrationsByDate.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            // Тесты по типам
            stats.TestsByType = new Dictionary<string, int>
    {
        { "Орфография", await _context.SpellingTests.CountAsync() },
        { "Классические", await _context.RegularTests.CountAsync() },
        { "Пунктуация", await _context.PunctuationTests.CountAsync() },
        { "Орфоэпия", await _context.OrthoeopyTests.CountAsync() }
    };

            // Результаты тестов за последние 30 дней
            var spellingResults = await _context.SpellingTestResults
                .Where(r => r.StartedAt >= thirtyDaysAgo && r.IsCompleted)
                .GroupBy(r => r.StartedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var regularResults = await _context.RegularTestResults
                .Where(r => r.StartedAt >= thirtyDaysAgo && r.IsCompleted)
                .GroupBy(r => r.StartedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var punctuationResults = await _context.PunctuationTestResults
                .Where(r => r.StartedAt >= thirtyDaysAgo && r.IsCompleted)
                .GroupBy(r => r.StartedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var orthoeopyResults = await _context.OrthoeopyTestResults
                .Where(r => r.StartedAt >= thirtyDaysAgo && r.IsCompleted)
                .GroupBy(r => r.StartedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var allResults = spellingResults
                .Concat(regularResults)
                .Concat(punctuationResults)
                .Concat(orthoeopyResults)
                .GroupBy(x => x.Date)
                .Select(g => new { Date = g.Key, Count = g.Sum(x => x.Count) })
                .OrderBy(x => x.Date)
                .ToDictionary(x => x.Date, x => x.Count);

            for (var date = thirtyDaysAgo.Date; date <= now.Date; date = date.AddDays(1))
            {
                if (!allResults.ContainsKey(date))
                {
                    allResults[date] = 0;
                }
            }
            stats.TestResultsByDate = allResults.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            // Средний балл по типам тестов
            var spellingAvg = await _context.SpellingTestResults
                .Where(r => r.IsCompleted)
                .AverageAsync(r => (double?)r.Percentage) ?? 0;

            var regularAvg = await _context.RegularTestResults
                .Where(r => r.IsCompleted)
                .AverageAsync(r => (double?)r.Percentage) ?? 0;

            var punctuationAvg = await _context.PunctuationTestResults
                .Where(r => r.IsCompleted)
                .AverageAsync(r => (double?)r.Percentage) ?? 0;

            var orthoeopyAvg = await _context.OrthoeopyTestResults
                .Where(r => r.IsCompleted)
                .AverageAsync(r => (double?)r.Percentage) ?? 0;

            stats.AverageScoresByType = new Dictionary<string, double>
    {
        { "Орфография", Math.Round(spellingAvg, 1) },
        { "Классические", Math.Round(regularAvg, 1) },
        { "Пунктуация", Math.Round(punctuationAvg, 1) },
        { "Орфоэпия", Math.Round(orthoeopyAvg, 1) }
    };

            // Действия администраторов
            var adminActions = await _context.AuditLogs
                .Where(al => al.CreatedAt >= thirtyDaysAgo)
                .GroupBy(al => al.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            stats.AdminActionsByType = adminActions.ToDictionary(x => x.Action, x => x.Count);

            // Активность по дням недели
            var activityByDay = await _context.AuditLogs
                .Where(al => al.CreatedAt >= thirtyDaysAgo)
                .GroupBy(al => al.CreatedAt.DayOfWeek)
                .Select(g => new { DayOfWeek = g.Key, Count = g.Count() })
                .ToListAsync();

            var dayNames = new[] { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
            stats.ActivityByDayOfWeek = dayNames.ToDictionary(
                day => day,
                day => activityByDay.FirstOrDefault(x => dayNames[(int)x.DayOfWeek] == day)?.Count ?? 0
            );

            // Активные/неактивные пользователи
            stats.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            stats.InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive);

            // Топ учителей
            var topTeachers = await _context.Teachers
                .Include(t => t.User)
                .Select(t => new
                {
                    Name = t.User.FullName,
                    TestsCount = t.User.CreatedTests.Count
                })
                .OrderByDescending(t => t.TestsCount)
                .Take(5)
                .ToListAsync();

            stats.TopTeachersByTests = topTeachers.ToDictionary(x => x.Name, x => x.TestsCount);

            // Топ студентов
            var topStudents = await _context.Students
                .Include(s => s.User)
                .Select(s => new
                {
                    Name = s.User.FullName,
                    ResultsCount = s.RegularTestResults.Count(r => r.IsCompleted) +
                                  s.SpellingTestResults.Count(r => r.IsCompleted) +
                                  s.PunctuationTestResults.Count(r => r.IsCompleted) +
                                  s.OrthoeopyTestResults.Count(r => r.IsCompleted)
                })
                .OrderByDescending(s => s.ResultsCount)
                .Take(5)
                .ToListAsync();

            stats.TopStudentsByResults = topStudents.ToDictionary(x => x.Name, x => x.ResultsCount);

            return View(stats);
        }

        #endregion

        #region Export Actions

        // GET: Admin/Export
        public IActionResult Export()
        {
            return View();
        }

        // Export Users
        [HttpGet]
        public async Task<IActionResult> ExportUsers(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportUsersToCSVAsync();
                    fileName = $"Users_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportUsersToExcelAsync();
                    fileName = $"Users_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Users",
                    AuditEntityTypes.System,
                    null,
                    $"Exported users to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                TempData["ErrorMessage"] = "Ошибка при экспорте пользователей.";
                return RedirectToAction(nameof(Users));
            }
        }

        // Export Teachers
        [HttpGet]
        public async Task<IActionResult> ExportTeachers(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportTeachersToCSVAsync();
                    fileName = $"Teachers_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportTeachersToExcelAsync();
                    fileName = $"Teachers_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Teachers",
                    AuditEntityTypes.System,
                    null,
                    $"Exported teachers to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting teachers");
                TempData["ErrorMessage"] = "Ошибка при экспорте учителей.";
                return RedirectToAction(nameof(Teachers));
            }
        }

        // Export Students
        [HttpGet]
        public async Task<IActionResult> ExportStudents(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportStudentsToCSVAsync();
                    fileName = $"Students_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportStudentsToExcelAsync();
                    fileName = $"Students_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Students",
                    AuditEntityTypes.System,
                    null,
                    $"Exported students to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting students");
                TempData["ErrorMessage"] = "Ошибка при экспорте студентов.";
                return RedirectToAction(nameof(Users));
            }
        }

        // Export Classes
        [HttpGet]
        public async Task<IActionResult> ExportClasses(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportClassesToCSVAsync();
                    fileName = $"Classes_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportClassesToExcelAsync();
                    fileName = $"Classes_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Classes",
                    AuditEntityTypes.System,
                    null,
                    $"Exported classes to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting classes");
                TempData["ErrorMessage"] = "Ошибка при экспорте классов.";
                return RedirectToAction(nameof(Classes));
            }
        }

        // Export Tests
        [HttpGet]
        public async Task<IActionResult> ExportTests(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportTestsToCSVAsync();
                    fileName = $"Tests_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportTestsToExcelAsync();
                    fileName = $"Tests_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Tests",
                    AuditEntityTypes.System,
                    null,
                    $"Exported tests to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting tests");
                TempData["ErrorMessage"] = "Ошибка при экспорте тестов.";
                return RedirectToAction(nameof(Tests));
            }
        }

        // Export Test Results
        [HttpGet]
        public async Task<IActionResult> ExportTestResults(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportTestResultsToCSVAsync();
                    fileName = $"TestResults_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportTestResultsToExcelAsync();
                    fileName = $"TestResults_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Test Results",
                    AuditEntityTypes.System,
                    null,
                    $"Exported test results to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting test results");
                TempData["ErrorMessage"] = "Ошибка при экспорте результатов.";
                return RedirectToAction(nameof(TestResults));
            }
        }

        // Export Audit Logs
        [HttpGet]
        public async Task<IActionResult> ExportAuditLogs(string format = "excel")
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportAuditLogsToCSVAsync();
                    fileName = $"AuditLogs_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportAuditLogsToExcelAsync();
                    fileName = $"AuditLogs_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Audit Logs",
                    AuditEntityTypes.System,
                    null,
                    $"Exported audit logs to {format.ToUpper()}",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs");
                TempData["ErrorMessage"] = "Ошибка при экспорте журнала.";
                return RedirectToAction(nameof(AuditLogs));
            }
        }

        // Export Full System
        [HttpGet]
        public async Task<IActionResult> ExportFullSystem()
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var adminName = User.Identity?.Name ?? "Unknown";

                var fileData = await _exportService.ExportFullSystemToExcelAsync();
                var fileName = $"FullSystem_Export_{DateTime.Now:yyyy-MM-dd}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                await _auditLogService.LogActionAsync(
                    adminId!,
                    adminName,
                    "Export Full System",
                    AuditEntityTypes.System,
                    null,
                    "Exported full system data to Excel",
                    GetIpAddress()
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting full system");
                TempData["ErrorMessage"] = "Ошибка при экспорте всех данных.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion


    }
}
