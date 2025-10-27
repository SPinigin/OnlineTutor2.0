using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CalendarController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Calendar
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем статистику
            var now = DateTime.Now;
            var upcomingEvents = await _context.CalendarEvents
                .Where(e => e.TeacherId == currentUser.Id &&
                           e.StartDateTime >= now &&
                           !e.IsCompleted)
                .CountAsync();

            var todayEvents = await _context.CalendarEvents
                .Where(e => e.TeacherId == currentUser.Id &&
                           e.StartDateTime.Date == now.Date)
                .CountAsync();

            var completedThisMonth = await _context.CalendarEvents
                .Where(e => e.TeacherId == currentUser.Id &&
                           e.IsCompleted &&
                           e.StartDateTime.Month == now.Month &&
                           e.StartDateTime.Year == now.Year)
                .CountAsync();

            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.TodayEvents = todayEvents;
            ViewBag.CompletedThisMonth = completedThisMonth;

            return View();
        }

        // GET: Calendar/Create
        public async Task<IActionResult> Create(DateTime? date, int? classId, int? studentId)
        {
            var model = new CreateCalendarEventViewModel
            {
                StartDateTime = date ?? DateTime.Now,
                EndDateTime = (date ?? DateTime.Now).AddHours(1),
                ClassId = classId,
                StudentId = studentId,
                Color = "#007bff"
            };

            await LoadSelectLists();
            return View(model);
        }

        // POST: Calendar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCalendarEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Валидация: должен быть выбран класс или ученик
                if (!model.ClassId.HasValue && !model.StudentId.HasValue)
                {
                    ModelState.AddModelError("", "Выберите класс или ученика для занятия");
                    await LoadSelectLists();
                    return View(model);
                }

                // Валидация: нельзя выбрать и класс и ученика одновременно
                if (model.ClassId.HasValue && model.StudentId.HasValue)
                {
                    ModelState.AddModelError("", "Выберите либо класс, либо ученика, но не оба");
                    await LoadSelectLists();
                    return View(model);
                }

                // Валидация дат
                if (model.EndDateTime <= model.StartDateTime)
                {
                    ModelState.AddModelError("EndDateTime", "Время окончания должно быть позже времени начала");
                    await LoadSelectLists();
                    return View(model);
                }

                var currentUser = await _userManager.GetUserAsync(User);

                // Проверяем, что класс или ученик принадлежит текущему учителю
                if (model.ClassId.HasValue)
                {
                    var classExists = await _context.Classes
                        .AnyAsync(c => c.Id == model.ClassId.Value && c.TeacherId == currentUser.Id);

                    if (!classExists)
                    {
                        TempData["ErrorMessage"] = "Указанный класс не найден";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (model.StudentId.HasValue)
                {
                    var studentExists = await _context.Students
                        .Include(s => s.Class)
                        .AnyAsync(s => s.Id == model.StudentId.Value &&
                                      s.Class != null &&
                                      s.Class.TeacherId == currentUser.Id);

                    if (!studentExists)
                    {
                        TempData["ErrorMessage"] = "Указанный ученик не найден или не состоит в вашем классе";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Дополнительная валидация повторяющихся событий
                if (model.IsRecurring && string.IsNullOrWhiteSpace(model.RecurrencePattern))
                {
                    ModelState.AddModelError("RecurrencePattern", "Выберите периодичность повторения для повторяющегося события");
                }

                model.StartDateTime = new DateTime(
                    model.StartDateTime.Year,
                    model.StartDateTime.Month,
                    model.StartDateTime.Day,
                    model.StartDateTime.Hour,
                    model.StartDateTime.Minute,
                    0
                );

                model.EndDateTime = new DateTime(
                    model.EndDateTime.Year,
                    model.EndDateTime.Month,
                    model.EndDateTime.Day,
                    model.EndDateTime.Hour,
                    model.EndDateTime.Minute,
                    0
                );

                var calendarEvent = new CalendarEvent
                {
                    Title = model.Title,
                    Description = model.Description,
                    StartDateTime = model.StartDateTime,
                    EndDateTime = model.EndDateTime,
                    TeacherId = currentUser.Id,
                    ClassId = model.ClassId,
                    StudentId = model.StudentId,
                    Location = model.Location,
                    Color = model.Color ?? "#007bff",
                    IsRecurring = model.IsRecurring,
                    RecurrencePattern = model.RecurrencePattern,
                    CreatedAt = DateTime.Now
                };

                _context.CalendarEvents.Add(calendarEvent);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Занятие успешно добавлено в календарь!";
                return RedirectToAction(nameof(Index));
            }

            await LoadSelectLists();
            return View(model);
        }

        // GET: Calendar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherId == currentUser.Id);

            if (calendarEvent == null) return NotFound();

            var model = new EditCalendarEventViewModel
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartDateTime = calendarEvent.StartDateTime,
                EndDateTime = calendarEvent.EndDateTime,
                ClassId = calendarEvent.ClassId,
                StudentId = calendarEvent.StudentId,
                Location = calendarEvent.Location,
                Color = calendarEvent.Color,
                IsRecurring = calendarEvent.IsRecurring,
                RecurrencePattern = calendarEvent.RecurrencePattern,
                IsCompleted = calendarEvent.IsCompleted,
                Notes = calendarEvent.Notes
            };

            await LoadSelectLists();
            return View(model);
        }

        // POST: Calendar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCalendarEventViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (!model.ClassId.HasValue && !model.StudentId.HasValue)
                {
                    ModelState.AddModelError("", "Выберите класс или ученика для занятия");
                    await LoadSelectLists();
                    return View(model);
                }

                if (model.ClassId.HasValue && model.StudentId.HasValue)
                {
                    ModelState.AddModelError("", "Выберите либо класс, либо ученика, но не оба");
                    await LoadSelectLists();
                    return View(model);
                }

                if (model.EndDateTime <= model.StartDateTime)
                {
                    ModelState.AddModelError("EndDateTime", "Время окончания должно быть позже времени начала");
                    await LoadSelectLists();
                    return View(model);
                }

                // Дополнительная валидация повторяющихся событий
                if (model.IsRecurring && string.IsNullOrWhiteSpace(model.RecurrencePattern))
                {
                    ModelState.AddModelError("RecurrencePattern", "Выберите периодичность повторения для повторяющегося события");
                }

                model.StartDateTime = new DateTime(
                    model.StartDateTime.Year,
                    model.StartDateTime.Month,
                    model.StartDateTime.Day,
                    model.StartDateTime.Hour,
                    model.StartDateTime.Minute,
                    0
                );

                model.EndDateTime = new DateTime(
                    model.EndDateTime.Year,
                    model.EndDateTime.Month,
                    model.EndDateTime.Day,
                    model.EndDateTime.Hour,
                    model.EndDateTime.Minute,
                    0
                );

                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var calendarEvent = await _context.CalendarEvents
                        .FirstOrDefaultAsync(e => e.Id == id && e.TeacherId == currentUser.Id);

                    if (calendarEvent == null) return NotFound();

                    calendarEvent.Title = model.Title;
                    calendarEvent.Description = model.Description;
                    calendarEvent.StartDateTime = model.StartDateTime;
                    calendarEvent.EndDateTime = model.EndDateTime;
                    calendarEvent.ClassId = model.ClassId;
                    calendarEvent.StudentId = model.StudentId;
                    calendarEvent.Location = model.Location;
                    calendarEvent.Color = model.Color ?? "#007bff";
                    calendarEvent.IsRecurring = model.IsRecurring;
                    calendarEvent.RecurrencePattern = model.RecurrencePattern;
                    calendarEvent.IsCompleted = model.IsCompleted;
                    calendarEvent.Notes = model.Notes;
                    calendarEvent.UpdatedAt = DateTime.Now;

                    _context.Update(calendarEvent);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Занятие успешно обновлено!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            await LoadSelectLists();
            return View(model);
        }

        // GET: Calendar/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Class)
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherId == currentUser.Id);

            if (calendarEvent == null) return NotFound();

            var model = new CalendarEventDetailsViewModel
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartDateTime = calendarEvent.StartDateTime,
                EndDateTime = calendarEvent.EndDateTime,
                ClassName = calendarEvent.Class?.Name,
                StudentName = calendarEvent.Student?.User.FullName,
                Location = calendarEvent.Location,
                Color = calendarEvent.Color,
                IsCompleted = calendarEvent.IsCompleted,
                Notes = calendarEvent.Notes,
                CreatedAt = calendarEvent.CreatedAt
            };

            return View(model);
        }

        // GET: Calendar/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Class)
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherId == currentUser.Id);

            if (calendarEvent == null) return NotFound();

            return View(calendarEvent);
        }

        // POST: Calendar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherId == currentUser.Id);

            if (calendarEvent == null) return NotFound();

            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Занятие удалено из календаря";
            return RedirectToAction(nameof(Index));
        }

        // GET: Calendar/GetEvents - для AJAX запросов (FullCalendar)
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime? start, DateTime? end)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var query = _context.CalendarEvents
                .Where(e => e.TeacherId == currentUser.Id)
                .Include(e => e.Class)
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .AsQueryable();

            if (start.HasValue)
                query = query.Where(e => e.EndDateTime >= start.Value);

            if (end.HasValue)
                query = query.Where(e => e.StartDateTime <= end.Value);

            var events = await query.ToListAsync();

            var result = events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                description = e.Description,
                className = e.Class?.Name,
                studentName = e.Student?.User.FullName,
                location = e.Location,
                color = e.Color,
                backgroundColor = e.Color,
                borderColor = e.Color,
                textColor = "#ffffff",
                isCompleted = e.IsCompleted,
                extendedProps = new
                {
                    classId = e.ClassId,
                    studentId = e.StudentId,
                    location = e.Location,
                    notes = e.Notes
                }
            });

            return Json(result);
        }

        // POST: Calendar/ToggleComplete/5
        [HttpPost]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherId == currentUser.Id);

            if (calendarEvent == null) return NotFound();

            calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
            calendarEvent.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = calendarEvent.IsCompleted ? "завершено" : "возвращено в активные";
            TempData["InfoMessage"] = $"Занятие \"{calendarEvent.Title}\" {status}";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Where(s => s.ClassId.HasValue &&
                           _context.Classes.Any(c => c.Id == s.ClassId && c.TeacherId == currentUser.Id))
                .OrderBy(s => s.User.LastName)
                .ToListAsync();

            ViewBag.Classes = new SelectList(classes, "Id", "Name");
            ViewBag.Students = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    Name = $"{s.User.FullName} ({s.Class.Name})"
                }), "Id", "Name");

            ViewBag.RecurrencePatterns = new SelectList(new[]
            {
                new { Value = "daily", Text = "Ежедневно" },
                new { Value = "weekly", Text = "Еженедельно" },
                new { Value = "biweekly", Text = "Раз в две недели" },
                new { Value = "monthly", Text = "Ежемесячно" }
            }, "Value", "Text");

            ViewBag.Colors = new[]
            {
                new { Value = "#007bff", Text = "Синий", Class = "primary" },
                new { Value = "#28a745", Text = "Зеленый", Class = "success" },
                new { Value = "#dc3545", Text = "Красный", Class = "danger" },
                new { Value = "#ffc107", Text = "Желтый", Class = "warning" },
                new { Value = "#17a2b8", Text = "Бирюзовый", Class = "info" },
                new { Value = "#6610f2", Text = "Фиолетовый", Class = "purple" },
                new { Value = "#e83e8c", Text = "Розовый", Class = "pink" },
                new { Value = "#6c757d", Text = "Серый", Class = "secondary" }
            };
        }
    }
}
