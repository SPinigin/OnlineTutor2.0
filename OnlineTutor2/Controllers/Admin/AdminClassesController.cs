using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для управления классами администратором
    /// </summary>
    public class AdminClassesController : AdminBaseController
    {
        private readonly IClassRepository _classRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IRegularTestClassRepository _regularTestClassRepository;
        private readonly ISpellingTestClassRepository _spellingTestClassRepository;
        private readonly IPunctuationTestClassRepository _punctuationTestClassRepository;
        private readonly IOrthoeopyTestClassRepository _orthoeopyTestClassRepository;
        private readonly IMaterialRepository _materialRepository;

        public AdminClassesController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminClassesController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IClassRepository classRepository,
            IStudentRepository studentRepository,
            IRegularTestClassRepository regularTestClassRepository,
            ISpellingTestClassRepository spellingTestClassRepository,
            IPunctuationTestClassRepository punctuationTestClassRepository,
            IOrthoeopyTestClassRepository orthoeopyTestClassRepository,
            IMaterialRepository materialRepository,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _classRepository = classRepository;
            _studentRepository = studentRepository;
            _regularTestClassRepository = regularTestClassRepository;
            _spellingTestClassRepository = spellingTestClassRepository;
            _punctuationTestClassRepository = punctuationTestClassRepository;
            _orthoeopyTestClassRepository = orthoeopyTestClassRepository;
            _materialRepository = materialRepository;
        }

        // GET: Admin/Classes
        public async Task<IActionResult> Index()
        {
            // Загружаем все классы, включая неактивные
            var classes = await _classRepository.GetAllAsync();
            classes = classes.OrderBy(c => c.Name).ToList();
            
            // Загружаем Teacher для каждого класса
            var teacherIds = classes.Select(c => c.TeacherId).Distinct().ToList();
            var teachers = new Dictionary<string, ApplicationUser>();
            
            foreach (var teacherId in teacherIds)
            {
                var teacher = await UserManager.FindByIdAsync(teacherId);
                if (teacher != null)
                {
                    teachers[teacherId] = teacher;
                }
            }
            
            // Присваиваем Teacher каждому классу
            foreach (var classItem in classes)
            {
                if (teachers.TryGetValue(classItem.TeacherId, out var teacher))
                {
                    classItem.Teacher = teacher;
                }
            }
            
            // Загружаем студентов для каждого класса
            foreach (var classItem in classes)
            {
                classItem.Students = await _studentRepository.GetByClassIdAsync(classItem.Id);
            }
            
            // Загружаем связи тестов
            var allRegularTestClasses = await _regularTestClassRepository.GetAllAsync();
            var allSpellingTestClasses = await _spellingTestClassRepository.GetAllAsync();
            var allPunctuationTestClasses = await _punctuationTestClassRepository.GetAllAsync();
            var allOrthoeopyTestClasses = await _orthoeopyTestClassRepository.GetAllAsync();
            
            foreach (var classItem in classes)
            {
                classItem.RegularTestClasses = allRegularTestClasses.Where(rtc => rtc.ClassId == classItem.Id).ToList();
                classItem.SpellingTestClasses = allSpellingTestClasses.Where(stc => stc.ClassId == classItem.Id).ToList();
                classItem.PunctuationTestClasses = allPunctuationTestClasses.Where(ptc => ptc.ClassId == classItem.Id).ToList();
                classItem.OrthoeopyTestClasses = allOrthoeopyTestClasses.Where(otc => otc.ClassId == classItem.Id).ToList();
            }
            
            // Загружаем материалы
            var allMaterials = await _materialRepository.GetAllAsync();
            foreach (var classItem in classes)
            {
                classItem.Materials = allMaterials.Where(m => m.ClassId == classItem.Id).ToList();
            }
            
            return View(classes);
        }

        // POST: Admin/Classes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var adminId = GetAdminId();
            var @class = await _classRepository.GetByIdAsync(id);

            if (@class == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий класс {ClassId}", adminId, id);
                return NotFound();
            }

            var className = @class.Name;
            
            // Получаем студентов класса
            var students = await _studentRepository.GetByClassIdAsync(id);
            var studentsCount = students.Count;

            // Получаем количество тестов
            var regularTestsCount = await Db.QueryScalarAsync<int>("SELECT COUNT(*) FROM RegularTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            var spellingTestsCount = await Db.QueryScalarAsync<int>("SELECT COUNT(*) FROM SpellingTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            var punctuationTestsCount = await Db.QueryScalarAsync<int>("SELECT COUNT(*) FROM PunctuationTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            var orthoeopyTestsCount = await Db.QueryScalarAsync<int>("SELECT COUNT(*) FROM OrthoeopyTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            var testsCount = regularTestsCount + spellingTestsCount + punctuationTestsCount + orthoeopyTestsCount;

            // Убираем студентов из класса
            foreach (var student in students)
            {
                student.ClassId = null;
                await _studentRepository.UpdateAsync(student);
            }

            // Удаляем связи тестов с классом
            await Db.ExecuteAsync("DELETE FROM RegularTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            await Db.ExecuteAsync("DELETE FROM SpellingTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            await Db.ExecuteAsync("DELETE FROM PunctuationTestClasses WHERE ClassId = @ClassId", new { ClassId = id });
            await Db.ExecuteAsync("DELETE FROM OrthoeopyTestClasses WHERE ClassId = @ClassId", new { ClassId = id });

            await _classRepository.DeleteAsync(id);

            await LogAdminActionAsync(
                AuditActions.ClassDeleted,
                AuditEntityTypes.Class,
                id.ToString(),
                $"Удален класс: {className} (Студентов: {studentsCount}, Тестов: {testsCount})"
            );

            Logger.LogInformation("Администратор {AdminId} удалил класс {ClassId}, Название: {ClassName}, Студентов: {StudentsCount}, Тестов: {TestsCount}",
                adminId, id, className, studentsCount, testsCount);

            SetSuccessMessage($"Класс {@class.Name} удален!");
            return RedirectToAction(nameof(Index));
        }
    }
}

