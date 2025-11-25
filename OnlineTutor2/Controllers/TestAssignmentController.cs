using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TestAssignmentController : Controller
    {
        private readonly ITestAssignmentRepository _testAssignmentRepository;
        private readonly ITestCategoryRepository _testCategoryRepository;
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TestAssignmentController> _logger;

        public TestAssignmentController(
            ITestAssignmentRepository testAssignmentRepository,
            ITestCategoryRepository testCategoryRepository,
            ISpellingTestRepository spellingTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            IRegularTestRepository regularTestRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<TestAssignmentController> logger)
        {
            _testAssignmentRepository = testAssignmentRepository;
            _testCategoryRepository = testCategoryRepository;
            _spellingTestRepository = spellingTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _regularTestRepository = regularTestRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: TestAssignment
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            
            var assignments = await _testAssignmentRepository.GetByTeacherIdAsync(currentUser.Id);
            
            // Загружаем категории для отображения
            var categories = await _testCategoryRepository.GetActiveAsync();
            
            // Загружаем все тесты один раз (оптимизация)
            var allSpellingTests = await _spellingTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var allPunctuationTests = await _punctuationTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var allOrthoeopyTests = await _orthoeopyTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var allRegularTests = await _regularTestRepository.GetByTeacherIdAsync(currentUser.Id);
            
            // Загружаем тесты для каждого задания
            foreach (var assignment in assignments)
            {
                assignment.TestCategory = categories.FirstOrDefault(c => c.Id == assignment.TestCategoryId);
                
                var assignmentId = assignment.Id;
                
                // Фильтруем тесты по TestAssignmentId
                assignment.SpellingTests = allSpellingTests.Where(t => t.TestAssignmentId == assignmentId).ToList();
                assignment.PunctuationTests = allPunctuationTests.Where(t => t.TestAssignmentId == assignmentId).ToList();
                assignment.OrthoeopyTests = allOrthoeopyTests.Where(t => t.TestAssignmentId == assignmentId).ToList();
                assignment.RegularTests = allRegularTests.Where(t => t.TestAssignmentId == assignmentId).ToList();
            }

            return View(assignments);
        }

        // GET: TestAssignment/Create
        public async Task<IActionResult> Create(int? testCategoryId = null)
        {
            var categories = await _testCategoryRepository.GetActiveAsync();
            ViewBag.TestCategories = new SelectList(categories, "Id", "Name", testCategoryId);
            
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Получаем следующий номер задания для выбранной категории
            if (testCategoryId.HasValue)
            {
                var existingAssignments = await _testAssignmentRepository.GetByTeacherIdAndCategoryAsync(
                    currentUser.Id, testCategoryId.Value);
                var nextNumber = existingAssignments.Any() 
                    ? existingAssignments.Max(a => a.AssignmentNumber) + 1 
                    : 1;
                ViewBag.NextAssignmentNumber = nextNumber;
            }
            else
            {
                ViewBag.NextAssignmentNumber = 1;
            }

            return View();
        }

        // POST: TestAssignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTestAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Попытка создания задания неавторизованным пользователем");
                    return Unauthorized();
                }
                
                // Проверяем, не существует ли уже задание с таким номером для этой категории
                var existing = await _testAssignmentRepository.GetByAssignmentNumberAsync(
                    model.AssignmentNumber, model.TestCategoryId, currentUser.Id);
                
                if (existing != null)
                {
                    ModelState.AddModelError("AssignmentNumber", 
                        "Задание с таким номером уже существует для выбранной категории тестов.");
                    var categories = await _testCategoryRepository.GetActiveAsync();
                    ViewBag.TestCategories = new SelectList(categories, "Id", "Name", model.TestCategoryId);
                    return View(model);
                }

                var assignment = new TestAssignment
                {
                    Title = model.Title,
                    Description = model.Description,
                    AssignmentNumber = model.AssignmentNumber,
                    TestCategoryId = model.TestCategoryId,
                    TeacherId = currentUser.Id,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                var assignmentId = await _testAssignmentRepository.CreateAsync(assignment);
                assignment.Id = assignmentId;
                
                _logger.LogInformation("Учитель {TeacherId} создал задание {AssignmentId}: {Title}",
                    currentUser.Id, assignment.Id, assignment.Title);

                TempData["SuccessMessage"] = $"Задание \"{assignment.Title}\" успешно создано!";
                return RedirectToAction(nameof(Index));
            }

            var categories2 = await _testCategoryRepository.GetActiveAsync();
            ViewBag.TestCategories = new SelectList(categories2, "Id", "Name", model.TestCategoryId);
            return View(model);
        }

        // GET: TestAssignment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            
            var assignment = await _testAssignmentRepository.GetByIdAsync(id.Value);
            
            if (assignment == null || assignment.TeacherId != currentUser.Id) return NotFound();

            var model = new CreateTestAssignmentViewModel
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                AssignmentNumber = assignment.AssignmentNumber,
                TestCategoryId = assignment.TestCategoryId,
                IsActive = assignment.IsActive
            };

            var categories = await _testCategoryRepository.GetActiveAsync();
            ViewBag.TestCategories = new SelectList(categories, "Id", "Name", assignment.TestCategoryId);
            
            return View(model);
        }

        // POST: TestAssignment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateTestAssignmentViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();
                
                var assignment = await _testAssignmentRepository.GetByIdAsync(id);
                
                if (assignment == null || assignment.TeacherId != currentUser.Id) return NotFound();

                // Проверяем, не существует ли уже задание с таким номером для этой категории (кроме текущего)
                var existing = await _testAssignmentRepository.GetByAssignmentNumberAsync(
                    model.AssignmentNumber, model.TestCategoryId, currentUser.Id);
                
                if (existing != null && existing.Id != id)
                {
                    ModelState.AddModelError("AssignmentNumber", 
                        "Задание с таким номером уже существует для выбранной категории тестов.");
                    var categories = await _testCategoryRepository.GetActiveAsync();
                    ViewBag.TestCategories = new SelectList(categories, "Id", "Name", model.TestCategoryId);
                    return View(model);
                }

                assignment.Title = model.Title;
                assignment.Description = model.Description;
                assignment.AssignmentNumber = model.AssignmentNumber;
                assignment.TestCategoryId = model.TestCategoryId;
                assignment.IsActive = model.IsActive;

                await _testAssignmentRepository.UpdateAsync(assignment);
                
                _logger.LogInformation("Учитель {TeacherId} обновил задание {AssignmentId}: {Title}",
                    currentUser.Id, assignment.Id, assignment.Title);

                TempData["SuccessMessage"] = $"Задание \"{assignment.Title}\" успешно обновлено!";
                return RedirectToAction(nameof(Index));
            }

            var categories2 = await _testCategoryRepository.GetActiveAsync();
            ViewBag.TestCategories = new SelectList(categories2, "Id", "Name", model.TestCategoryId);
            return View(model);
        }

        // GET: TestAssignment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var assignment = await _testAssignmentRepository.GetByIdAsync(id.Value);
            
            if (assignment == null || assignment.TeacherId != currentUser.Id) return NotFound();

            var categories = await _testCategoryRepository.GetActiveAsync();
            assignment.TestCategory = categories.FirstOrDefault(c => c.Id == assignment.TestCategoryId);

            return View(assignment);
        }

        // POST: TestAssignment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var assignment = await _testAssignmentRepository.GetByIdAsync(id);
            
            if (assignment == null || assignment.TeacherId != currentUser.Id) return NotFound();

            await _testAssignmentRepository.DeleteAsync(id);
            
            _logger.LogInformation("Учитель {TeacherId} удалил задание {AssignmentId}: {Title}",
                currentUser.Id, assignment.Id, assignment.Title);

            TempData["SuccessMessage"] = $"Задание \"{assignment.Title}\" успешно удалено!";
            return RedirectToAction(nameof(Index));
        }
    }
}

