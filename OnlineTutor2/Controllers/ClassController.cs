using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class ClassController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Class
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id)
                .Include(c => c.Students)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(classes);
        }

        // GET: Class/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                    .ThenInclude(s => s.User)
                .Include(c => c.Tests)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        // GET: Class/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Class/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClassViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var @class = new Class
                {
                    Name = model.Name,
                    Description = model.Description,
                    TeacherId = currentUser.Id
                };

                _context.Classes.Add(@class);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно создан!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Class/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            var model = new EditClassViewModel
            {
                Id = @class.Id,
                Name = @class.Name,
                Description = @class.Description,
                IsActive = @class.IsActive
            };

            return View(model);
        }

        // POST: Class/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditClassViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var @class = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

                    if (@class == null) return NotFound();

                    @class.Name = model.Name;
                    @class.Description = model.Description;
                    @class.IsActive = model.IsActive;

                    _context.Update(@class);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            return View(model);
        }

        // GET: Class/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .Include(c => c.Students)
                .Include(c => c.Tests)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        // POST: Class/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            // Проверяем, есть ли ученики в классе
            if (@class.Students.Any())
            {
                TempData["ErrorMessage"] = "Нельзя удалить класс, в котором есть ученики. Сначала переместите учеников в другой класс или удалите их.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Class/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            @class.IsActive = !@class.IsActive;
            await _context.SaveChangesAsync();

            var status = @class.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Класс \"{@class.Name}\" {status}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
