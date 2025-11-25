using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;
using OnlineTutor2.ViewModels;
using IStatisticsRepository = OnlineTutor2.Data.Repositories.IStatisticsRepository;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для управления пользователями администратором
    /// </summary>
    public class AdminUsersController : AdminBaseController
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly IGradeRepository _gradeRepository;

        public AdminUsersController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminUsersController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IStudentRepository studentRepository,
            ITeacherRepository teacherRepository,
            IRegularTestResultRepository regularTestResultRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            IGradeRepository gradeRepository,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _gradeRepository = gradeRepository;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(string? searchString, string? roleFilter, string? sortOrder)
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.EmailSortParm = sortOrder == "Email" ? "email_desc" : "Email";

            // Получаем пользователей через SQL с фильтрацией и сортировкой
            var sql = "SELECT * FROM AspNetUsers WHERE 1=1";
            var parameters = new Dictionary<string, object>();

            // Фильтрация по поиску
            if (!string.IsNullOrEmpty(searchString))
            {
                sql += " AND (FirstName LIKE @SearchString OR LastName LIKE @SearchString OR Email LIKE @SearchString)";
                parameters["SearchString"] = $"%{searchString}%";
            }

            // Сортировка
            switch (sortOrder)
            {
                case "name_desc":
                    sql += " ORDER BY LastName DESC";
                    break;
                case "Email":
                    sql += " ORDER BY Email ASC";
                    break;
                case "email_desc":
                    sql += " ORDER BY Email DESC";
                    break;
                case "Date":
                    sql += " ORDER BY CreatedAt ASC";
                    break;
                case "date_desc":
                    sql += " ORDER BY CreatedAt DESC";
                    break;
                default:
                    sql += " ORDER BY LastName ASC";
                    break;
            }

            var usersList = await Db.QueryAsync<ApplicationUser>(sql, parameters.Count > 0 ? parameters : null);
            var usersWithRoles = new List<AdminUserViewModel>();

            foreach (var user in usersList)
            {
                var roles = await UserManager.GetRolesAsync(user);
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

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await UserManager.GetRolesAsync(user);
            var student = await _studentRepository.GetByUserIdAsync(id);
            var teacher = await _teacherRepository.GetByUserIdAsync(id);

            var viewModel = new AdminUserDetailsViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Student = student,
                Teacher = teacher
            };

            return View(viewModel);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var adminId = GetAdminId();
            var user = await UserManager.FindByIdAsync(id);

            if (user == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующего пользователя {UserId}", adminId, id);
                return NotFound();
            }

            var userName = user.FullName;
            var userEmail = user.Email;

            // Удаляем связанные данные
            var student = await _studentRepository.GetByUserIdAsync(id);
            if (student != null)
            {
                // Удаляем результаты тестов через SQL
                await Db.ExecuteAsync("DELETE FROM RegularTestResults WHERE StudentId = @StudentId", new { StudentId = student.Id });
                await Db.ExecuteAsync("DELETE FROM SpellingTestResults WHERE StudentId = @StudentId", new { StudentId = student.Id });
                await Db.ExecuteAsync("DELETE FROM PunctuationTestResults WHERE StudentId = @StudentId", new { StudentId = student.Id });
                await Db.ExecuteAsync("DELETE FROM OrthoeopyTestResults WHERE StudentId = @StudentId", new { StudentId = student.Id });
                await Db.ExecuteAsync("DELETE FROM Grades WHERE StudentId = @StudentId", new { StudentId = student.Id });
                await _studentRepository.DeleteAsync(student.Id);

                Logger.LogInformation("Удалены связанные данные студента {StudentId}: результаты тестов, оценки", student.Id);
            }

            var teacher = await _teacherRepository.GetByUserIdAsync(id);
            if (teacher != null)
            {
                await _teacherRepository.DeleteAsync(teacher.Id);
                Logger.LogInformation("Удален профиль учителя {TeacherId}", teacher.Id);
            }

            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await LogAdminActionAsync(
                    AuditActions.UserDeleted,
                    AuditEntityTypes.User,
                    id,
                    $"Deleted user: {userName} ({userEmail})"
                );
                Logger.LogInformation("Администратор {AdminId} успешно удалил пользователя {UserId}, Email: {Email}", adminId, id, user.Email);
                SetSuccessMessage($"Пользователь {user.FullName} успешно удален!");
            }
            else
            {
                Logger.LogError("Ошибка удаления пользователя {UserId} администратором {AdminId}. Errors: {@Errors}", id, adminId, result.Errors);
                SetErrorMessage("Ошибка при удалении пользователя.");
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var adminId = GetAdminId();
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался изменить статус несуществующего пользователя {UserId}", adminId, id);
                return NotFound();
            }

            var oldStatus = user.IsActive;
            user.IsActive = !user.IsActive;
            var result = await UserManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await LogAdminActionAsync(
                    user.IsActive ? AuditActions.UserActivated : AuditActions.UserDeactivated,
                    AuditEntityTypes.User,
                    id,
                    $"{(user.IsActive ? "Activated" : "Deactivated")} user: {user.FullName}"
                );
                var status = user.IsActive ? "активирован" : "заблокирован";
                Logger.LogInformation("Администратор {AdminId} изменил статус пользователя {UserId} с {OldStatus} на {NewStatus}", adminId, id, oldStatus, user.IsActive);
                SetInfoMessage($"Пользователь {user.FullName} {status}.");
            }
            else
            {
                Logger.LogError("Ошибка изменения статуса пользователя {UserId} администратором {AdminId}. Errors: {@Errors}", id, adminId, result.Errors);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

