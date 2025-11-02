using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
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

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
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
                TotalRegularTests = await _context.Tests.CountAsync(),
                TotalTestResults = await _context.SpellingTestResults.CountAsync() + await _context.TestResults.CountAsync(),
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
                .Include(s => s.TestResults)
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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующего пользователя {UserId}", adminId, id);
                return NotFound();
            }
            // Удаляем связанные данные
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == id);
            if (student != null)
            {
                var testResults = _context.TestResults.Where(tr => tr.StudentId == student.Id);
                var spellingResults = _context.SpellingTestResults.Where(str => str.StudentId == student.Id);
                var grades = _context.Grades.Where(g => g.StudentId == student.Id);

                _context.TestResults.RemoveRange(testResults);
                _context.SpellingTestResults.RemoveRange(spellingResults);
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
            await _context.SaveChangesAsync();

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
                .Include(c => c.Tests)
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
                .Include(c => c.Tests)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (@class == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий класс {ClassId}", adminId, id);
                return NotFound();
            }

            var className = @class.Name;
            var studentsCount = @class.Students.Count;
            var testsCount = @class.Tests.Count;

            // Убираем студентов из класса
            foreach (var student in @class.Students)
            {
                student.ClassId = null;
            }

            // Убираем привязку тестов к классу
            foreach (var test in @class.Tests)
            {
                test.ClassId = null;
            }

            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();

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
                .Include(st => st.Questions)
                .Include(st => st.TestResults)
                .Select(st => new AdminTestViewModel
                {
                    Id = st.Id,
                    Title = st.Title,
                    Type = "Орфография",
                    TeacherName = st.Teacher.FullName,
                    ClassName = st.Class != null ? st.Class.Name : "Все ученики",
                    QuestionsCount = st.Questions.Count,
                    ResultsCount = st.TestResults.Count,
                    CreatedAt = st.CreatedAt,
                    IsActive = st.IsActive,
                    ControllerName = "SpellingTest"
                })
                .ToListAsync();

            var regularTests = await _context.Tests
                .Include(t => t.Teacher)
                .Include(t => t.Class)
                .Include(t => t.Questions)
                .Include(t => t.TestResults)
                .Select(t => new AdminTestViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = "Обычный",
                    TeacherName = t.Teacher.FullName,
                    ClassName = t.Class != null ? t.Class.Name : "Все ученики",
                    QuestionsCount = t.Questions.Count,
                    ResultsCount = t.TestResults.Count,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive,
                    ControllerName = "Test"
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
                .Include(st => st.TestResults)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (test == null)
            {
                _logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий тест орфографии {TestId}", adminId, id);
                return NotFound();
            }

            var testTitle = test.Title;
            var resultsCount = test.TestResults.Count;

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
            var test = await _context.Tests
                .Include(t => t.TestResults)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            _context.Tests.Remove(test);
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

            var regularResults = await _context.TestResults
                .Include(tr => tr.Test)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Select(tr => new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = tr.Test.Title,
                    TestType = "Обычный",
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

            var allResults = spellingResults.Concat(regularResults)
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
            var result = await _context.TestResults.FindAsync(id);
            if (result == null) return NotFound();

            _context.TestResults.Remove(result);
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
            _logger.LogWarning("Администратор {AdminId} начал очистку всех результатов тестов", adminId);

            var spellingResults = await _context.SpellingTestResults.ToListAsync();
            var regularResults = await _context.TestResults.ToListAsync();
            var punctuationResults = await _context.PunctuationTestResults.ToListAsync();
            var orthoeopyResults = await _context.OrthoeopyTestResults.ToListAsync();

            var totalCount = spellingResults.Count + regularResults.Count + punctuationResults.Count + orthoeopyResults.Count;

            _context.SpellingTestResults.RemoveRange(spellingResults);
            _context.TestResults.RemoveRange(regularResults);
            _context.PunctuationTestResults.RemoveRange(punctuationResults);
            _context.OrthoeopyTestResults.RemoveRange(orthoeopyResults);

            await _context.SaveChangesAsync();

            _logger.LogWarning("Администратор {AdminId} удалил все результаты тестов. Всего удалено: {TotalCount} (Орфография: {SpellingCount}, " +
                "Обычные: {RegularCount}, Пунктуация: {PunctuationCount}, Орфоэпия: {OrthoeopyCount})", adminId, totalCount, spellingResults.Count, 
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
                    TotalRegularTests = await _context.Tests.CountAsync(),
                    TotalSpellingQuestions = await _context.SpellingQuestions.CountAsync(),
                    TotalRegularQuestions = await _context.Questions.CountAsync(),
                    TotalSpellingResults = await _context.SpellingTestResults.CountAsync(),
                    TotalRegularResults = await _context.TestResults.CountAsync(),
                    TotalMaterials = await _context.Materials.CountAsync()
                }
            };

            return View(systemInfo);
        }

        #endregion
    }
}
