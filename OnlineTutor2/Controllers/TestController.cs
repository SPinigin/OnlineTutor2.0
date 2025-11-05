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
                    case 1: // Тесты на орфография
                        category.SpellingTests = await _context.SpellingTests
                            .Where(st => st.TeacherId == currentUser.Id && st.TestCategoryId == category.Id)
                            .ToListAsync();
                        break;
                    case 2: // Тесты на пунктуацию
                        category.PunctuationTests = await _context.PunctuationTests
                            .Where(pt => pt.TeacherId == currentUser.Id && pt.TestCategoryId == category.Id)
                            .ToListAsync();
                        break;
                    case 3: // Тесты на орфоэпию
                        category.OrthoeopyTests = await _context.OrthoeopyTests
                            .Where(ot => ot.TeacherId == currentUser.Id && ot.TestCategoryId == category.Id)
                            .ToListAsync();
                        break;
                    case 4: // Тесты классические
                        category.RegularTests = await _context.RegularTests
                            .Where(ot => ot.TeacherId == currentUser.Id && ot.TestCategoryId == category.Id)
                            .ToListAsync();
                        break;
                    //case 5: // Свободные ответы
                    //    category.RegularTests = await _context.RegularTests
                    //        .Where(ot => ot.TeacherId == currentUser.Id && ot.TestCategoryId == category.Id)
                    //        .ToListAsync();
                    //    break;
                    //case 6: // Средства выразительности
                    //    category.RegularTests = await _context.RegularTests
                    //        .Where(ot => ot.TeacherId == currentUser.Id && ot.TestCategoryId == category.Id)
                    //        .ToListAsync();
                    //    break;
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

            switch (id)
            {
                case 1: // Тесты по орфографии
                    var spellingTests = await _context.SpellingTests
                        .Where(st => st.TeacherId == currentUser.Id && st.TestCategoryId == id)
                        .Include(st => st.Class)
                        .Include(st => st.SpellingQuestions)
                        .Include(st => st.SpellingTestResults)
                        .OrderByDescending(st => st.CreatedAt)
                        .ToListAsync();
                    return View("SpellingTests", spellingTests);

                case 2: // Тесты по пунктуации
                    var punctuationTests = await _context.PunctuationTests
                        .Where(pt => pt.TeacherId == currentUser.Id && pt.TestCategoryId == id)
                        .Include(pt => pt.Class)
                        .Include(pt => pt.PunctuationQuestions)
                        .Include(pt => pt.PunctuationTestResults)
                        .OrderByDescending(pt => pt.CreatedAt)
                        .ToListAsync();
                    return View("PunctuationTests", punctuationTests);

                case 3: // Тесты по орфоэпии
                    var orthoeopyTests = await _context.OrthoeopyTests
                        .Where(ot => ot.TeacherId == currentUser.Id && ot.TestCategoryId == id)
                        .Include(ot => ot.Class)
                        .Include(ot => ot.OrthoeopyQuestions)
                        .Include(ot => ot.OrthoeopyTestResults)
                        .OrderByDescending(ot => ot.CreatedAt)
                        .ToListAsync();
                    return View("OrthoeopyTests", orthoeopyTests);

                case 4: // Тесты классические
                    var regularTests = await _context.RegularTests
                        .Where(rt => rt.TeacherId == currentUser.Id && rt.TestCategoryId == id)
                        .Include(rt => rt.Class)
                        .Include(rt => rt.RegularQuestions)
                        .Include(rt => rt.RegularTestResults)
                        .OrderByDescending(rt => rt.CreatedAt)
                        .ToListAsync();
                    return View("RegularTests", regularTests);

                //case 5: // Тесты со свободными ответами
                //    var regularTests = await _context.RegularTests
                //        .Where(rt => rt.TeacherId == currentUser.Id && rt.TestCategoryId == id)
                //        .Include(rt => rt.Class)
                //        .Include(rt => rt.RegularQuestions)
                //        .Include(rt => rt.RegularTestResults)
                //        .OrderByDescending(rt => rt.CreatedAt)
                //        .ToListAsync();
                //    return View("RegularTests", regularTests);

                //case 6: // Тесты по средствам выразительности
                //    var orthoeopyTests = await _context.OrthoeopyTests
                //        .Where(ot => ot.TeacherId == currentUser.Id && ot.TestCategoryId == id)
                //        .Include(ot => ot.Class)
                //        .Include(ot => ot.OrthoeopyQuestions)
                //        .Include(ot => ot.OrthoeopyTestResults)
                //        .OrderByDescending(ot => ot.CreatedAt)
                //        .ToListAsync();
                //    return View("OrthoeopyTests", orthoeopyTests);

                default:
                    return View("EmptyCategory");
            }
        }
    }
}
