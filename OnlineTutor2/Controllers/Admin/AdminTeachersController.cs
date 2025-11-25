using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для управления учителями администратором
    /// </summary>
    public class AdminTeachersController : AdminBaseController
    {
        private readonly ITeacherRepository _teacherRepository;

        public AdminTeachersController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminTeachersController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            ITeacherRepository teacherRepository,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _teacherRepository = teacherRepository;
        }

        // GET: Admin/Teachers
        public async Task<IActionResult> Index()
        {
            var teachers = await _teacherRepository.GetAllWithUserAsync();
            // Сортируем в памяти по фамилии пользователя
            var teachersWithUsers = new List<Teacher>();
            foreach (var teacher in teachers)
            {
                var user = await UserManager.FindByIdAsync(teacher.UserId);
                if (user != null)
                {
                    teachersWithUsers.Add(teacher);
                }
            }
            teachersWithUsers = teachersWithUsers.OrderBy(t => UserManager.FindByIdAsync(t.UserId).Result?.LastName ?? "").ToList();

            return View(teachersWithUsers);
        }

        // POST: Admin/Teachers/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = GetAdminId();
            var teacher = await _teacherRepository.GetByIdAsync(id);

            if (teacher == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался одобрить несуществующего учителя {TeacherId}", adminId, id);
                return NotFound();
            }

            teacher.IsApproved = true;
            await _teacherRepository.UpdateAsync(teacher);

            var user = await UserManager.FindByIdAsync(teacher.UserId);
            var fullName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";

            await LogAdminActionAsync(
                AuditActions.TeacherApproved,
                AuditEntityTypes.Teacher,
                id.ToString(),
                $"Approved teacher: {fullName}"
            );

            Logger.LogInformation("Администратор {AdminId} одобрил учителя {TeacherId}, UserId: {UserId}, FullName: {FullName}, Email: {Email}",
                adminId, id, teacher.UserId, fullName, user?.Email ?? "");
            SetSuccessMessage($"Учитель {fullName} одобрен!");

            return RedirectToAction(nameof(Index));
        }
    }
}

