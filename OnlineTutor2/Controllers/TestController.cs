using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Test - Главная страница с категориями тестов
        public async Task<IActionResult> Index()
        {
            var categories = await _context.TestCategories
                .Where(tc => tc.IsActive)
                .OrderBy(tc => tc.OrderIndex)
                .ToListAsync();

            // Получаем количество тестов для каждой категории текущего учителя
            var currentUser = await _userManager.GetUserAsync(User);

            foreach (var category in categories)
            {
                switch (category.Id)
                {
                    case 1: // Тесты на правописание
                        category.SpellingTests = await _context.SpellingTests
                            .Where(st => st.TeacherId == currentUser.Id && st.TestCategoryId == category.Id)
                            .ToListAsync();
                        break;
                        // Здесь можно добавить другие типы тестов
                }
            }

            return View(categories);
        }

        // GET: Test/Category/1 - Тесты конкретной категории
        public async Task<IActionResult> Category(int id)
        {
            var category = await _context.TestCategories
                .FirstOrDefaultAsync(tc => tc.Id == id && tc.IsActive);

            if (category == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            ViewBag.Category = category;

            // В зависимости от категории загружаем соответствующие тесты
            switch (id)
            {
                case 1: // Тесты на правописание
                    var spellingTests = await _context.SpellingTests
                        .Where(st => st.TeacherId == currentUser.Id && st.TestCategoryId == id)
                        .Include(st => st.Class)
                        .Include(st => st.Questions)
                        .Include(st => st.TestResults)
                        .OrderByDescending(st => st.CreatedAt)
                        .ToListAsync();
                    return View("SpellingTests", spellingTests);

                // добавить другие case для других типов тестов
                default:
                    return View("EmptyCategory");
            }
        }
    }
}
