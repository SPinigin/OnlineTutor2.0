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
    public class CalendarController : Controller
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IClassRepository _classRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(
            ICalendarEventRepository calendarEventRepository,
            IClassRepository classRepository,
            IStudentRepository studentRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<CalendarController> logger)
        {
            _calendarEventRepository = calendarEventRepository;
            _classRepository = classRepository;
            _studentRepository = studentRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Calendar
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем статистику
            var now = DateTime.Now;
            var upcomingEvents = await _calendarEventRepository.GetUpcomingCountAsync(currentUser.Id, now);
            var todayEvents = await _calendarEventRepository.GetTodayCountAsync(currentUser.Id, now);
            var completedThisMonth = await _calendarEventRepository.GetCompletedThisMonthCountAsync(currentUser.Id, now);

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
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                // Валидация: должен быть выбран класс или ученик
                if (!model.ClassId.HasValue && !model.StudentId.HasValue)
                {
                    _logger.LogWarning("Учитель {TeacherId} попытался создать событие без класса и ученика", currentUser.Id);
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

                // Проверяем, что класс или ученик принадлежит текущему учителю
                if (model.ClassId.HasValue)
                {
                    var @class = await _classRepository.GetByIdAsync(model.ClassId.Value);
                    if (@class == null || @class.TeacherId != currentUser.Id)
                    {
                        TempData["ErrorMessage"] = "Указанный класс не найден";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (model.StudentId.HasValue)
                {
                    var student = await _studentRepository.GetByIdAsync(model.StudentId.Value);
                    if (student == null || !student.ClassId.HasValue)
                    {
                        TempData["ErrorMessage"] = "Указанный ученик не найден или не состоит в вашем классе";
                        return RedirectToAction(nameof(Index));
                    }
                    var studentClass = await _classRepository.GetByIdAsync(student.ClassId.Value);
                    if (studentClass == null || studentClass.TeacherId != currentUser.Id)
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

                await _calendarEventRepository.CreateAsync(calendarEvent);

                _logger.LogInformation("Учитель {TeacherId} создал событие {EventId}: {Title}, Дата: {StartDateTime}, ClassId: {ClassId}, StudentId: {StudentId}, IsRecurring: {IsRecurring}",
                    currentUser.Id, calendarEvent.Id, model.Title, model.StartDateTime, model.ClassId, model.StudentId, model.IsRecurring);

                TempData["SuccessMessage"] = "Занятие успешно добавлено в календарь!";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму создания события", currentUser.Id);

            await LoadSelectLists();
            return View(model);
        }

        // GET: Calendar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _calendarEventRepository.GetByIdWithRelationsAsync(id.Value, currentUser.Id);

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

            var currentUser = await _userManager.GetUserAsync(User);

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
                    var calendarEvent = await _calendarEventRepository.GetByIdWithRelationsAsync(id, currentUser.Id);

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

                    await _calendarEventRepository.UpdateAsync(calendarEvent);

                    _logger.LogInformation("Учитель {TeacherId} обновил событие {EventId}: {Title}, ClassId: {ClassId}, StudentId: {StudentId}",
                       currentUser.Id, id, model.Title, model.ClassId, model.StudentId);

                    TempData["SuccessMessage"] = "Занятие успешно обновлено!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении события {EventId} учителем {TeacherId}", id, currentUser.Id);
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
            var calendarEvent = await _calendarEventRepository.GetByIdWithRelationsAsync(id.Value, currentUser.Id);

            if (calendarEvent == null) return NotFound();

            // Загружаем связанные данные отдельно
            var @class = calendarEvent.ClassId.HasValue ? await _classRepository.GetByIdAsync(calendarEvent.ClassId.Value) : null;
            var student = calendarEvent.StudentId.HasValue ? await _studentRepository.GetWithUserAsync(calendarEvent.StudentId.Value) : null;

            var model = new CalendarEventDetailsViewModel
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartDateTime = calendarEvent.StartDateTime,
                EndDateTime = calendarEvent.EndDateTime,
                ClassName = @class?.Name,
                StudentName = student?.User?.FullName,
                Location = calendarEvent.Location,
                Color = calendarEvent.Color,
                IsCompleted = calendarEvent.IsCompleted,
                Notes = calendarEvent.Notes,
                CreatedAt = calendarEvent.CreatedAt,
                IsRecurring = calendarEvent.IsRecurring,
                RecurrencePattern = calendarEvent.RecurrencePattern 
            };

            return View(model);
        }

        // GET: Calendar/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _calendarEventRepository.GetByIdWithRelationsAsync(id.Value, currentUser.Id);

            if (calendarEvent == null) return NotFound();

            // Загружаем связанные данные отдельно для отображения
            var @class = calendarEvent.ClassId.HasValue ? await _classRepository.GetByIdAsync(calendarEvent.ClassId.Value) : null;
            var student = calendarEvent.StudentId.HasValue ? await _studentRepository.GetWithUserAsync(calendarEvent.StudentId.Value) : null;

            return View(calendarEvent);
        }

        // POST: Calendar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _calendarEventRepository.GetByIdWithRelationsAsync(id, currentUser.Id);

            if (calendarEvent == null) return NotFound();

            var eventTitle = calendarEvent.Title;
            await _calendarEventRepository.DeleteAsync(id);

            _logger.LogInformation("Учитель {TeacherId} удалил событие {EventId}: {Title}", currentUser.Id, id, eventTitle);

            TempData["SuccessMessage"] = "Занятие удалено из календаря";
            return RedirectToAction(nameof(Index));
        }

        // GET: Calendar/GetEvents - для AJAX запросов (FullCalendar)
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime? start, DateTime? end)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Расширяем диапазон поиска для повторяющихся событий
            var searchStart = start ?? DateTime.Now.AddMonths(-3);
            var searchEnd = end ?? DateTime.Now.AddMonths(6);

            // Получаем события, которые попадают в диапазон или повторяются
            var events = await _calendarEventRepository.GetByTeacherIdInDateRangeAsync(currentUser.Id, searchStart, searchEnd);

            var result = new List<object>();

            foreach (var e in events)
            {
                if (e.IsRecurring && !string.IsNullOrEmpty(e.RecurrencePattern))
                {
                    // Генерируем повторяющиеся события
                    var recurringEvents = await GenerateRecurringEventsAsync(e, searchStart, searchEnd);
                    result.AddRange(recurringEvents);
                }
                else
                {
                    // Обычное событие
                    result.Add(await CreateEventObjectAsync(e));
                }
            }

            return Json(result);
        }

        // Метод для создания объекта события
        private async Task<object> CreateEventObjectAsync(CalendarEvent e, DateTime? overrideStart = null, DateTime? overrideEnd = null)
        {
            var startTime = overrideStart ?? e.StartDateTime;
            var endTime = overrideEnd ?? e.EndDateTime;

            // Загружаем связанные данные
            var @class = e.ClassId.HasValue ? await _classRepository.GetByIdAsync(e.ClassId.Value) : null;
            var student = e.StudentId.HasValue ? await _studentRepository.GetWithUserAsync(e.StudentId.Value) : null;

            return new
            {
                id = e.Id,
                title = await GetEventTitleAsync(e),
                start = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = endTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                description = e.Description,
                className = @class?.Name,
                studentName = student?.User?.FullName,
                location = e.Location,
                color = e.Color,
                backgroundColor = e.Color,
                borderColor = e.Color,
                textColor = "#ffffff",
                isCompleted = e.IsCompleted,
                isRecurring = e.IsRecurring,
                extendedProps = new
                {
                    classId = e.ClassId,
                    studentId = e.StudentId,
                    location = e.Location,
                    notes = e.Notes,
                    originalTitle = e.Title,
                    isRecurringInstance = overrideStart.HasValue
                }
            };
        }

        // Генерация повторяющихся событий
        private async Task<List<object>> GenerateRecurringEventsAsync(CalendarEvent calendarEvent, DateTime rangeStart, DateTime rangeEnd)
        {
            var events = new List<object>();
            var duration = calendarEvent.EndDateTime - calendarEvent.StartDateTime;

            var currentDate = calendarEvent.StartDateTime;

            // Защита от бесконечного цикла
            var maxIterations = 365; // максимум 365 повторений
            var iterations = 0;

            while (currentDate <= rangeEnd && iterations < maxIterations)
            {
                iterations++;

                // Проверяем, попадает ли событие в запрошенный диапазон
                if (currentDate >= rangeStart && currentDate <= rangeEnd)
                {
                    var eventEnd = currentDate.Add(duration);
                    events.Add(await CreateEventObjectAsync(calendarEvent, currentDate, eventEnd));
                }

                // Вычисляем следующую дату на основе паттерна
                currentDate = GetNextOccurrence(currentDate, calendarEvent.RecurrencePattern);

                // Если следующая дата раньше текущей, значит ошибка - выходим
                if (currentDate <= calendarEvent.StartDateTime)
                {
                    break;
                }
            }

            return events;
        }

        // Вычисление следующего повторения
        private DateTime GetNextOccurrence(DateTime currentDate, string recurrencePattern)
        {
            return recurrencePattern?.ToLower() switch
            {
                "daily" => currentDate.AddDays(1),
                "weekly" => currentDate.AddDays(7),
                "biweekly" => currentDate.AddDays(14),
                "monthly" => currentDate.AddMonths(1),
                _ => currentDate.AddDays(7) // По умолчанию еженедельно
            };
        }

        // Вспомогательный метод для формирования названия события
        private async Task<string> GetEventTitleAsync(CalendarEvent calendarEvent)
        {
            var title = calendarEvent.Title;

            if (calendarEvent.ClassId.HasValue)
            {
                var @class = await _classRepository.GetByIdAsync(calendarEvent.ClassId.Value);
                if (@class != null)
                {
                    // Для класса только название класса: "5А"
                    return @class.Name;
                }
            }
            else if (calendarEvent.StudentId.HasValue)
            {
                var student = await _studentRepository.GetWithUserAsync(calendarEvent.StudentId.Value);
                if (student?.User != null)
                {
                    // Для ученика только фамилия: Пинигин С."
                    var lastName = student.User.LastName;
                    var firstNameInitial = student.User.FirstName?.FirstOrDefault();
                    return $"{lastName} {firstNameInitial}.";
                }
            }

            return title;
        }

        // POST: Calendar/ToggleComplete/5
        [HttpPost]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var calendarEvent = await _calendarEventRepository.GetByIdWithRelationsAsync(id, currentUser.Id);

            if (calendarEvent == null) return NotFound();

            var oldStatus = calendarEvent.IsCompleted;
            calendarEvent.IsCompleted = !calendarEvent.IsCompleted;
            calendarEvent.UpdatedAt = DateTime.Now;

            await _calendarEventRepository.UpdateAsync(calendarEvent);

            _logger.LogInformation("Учитель {TeacherId} изменил статус события {EventId} с {OldStatus} на {NewStatus}",
                currentUser.Id, id, oldStatus, calendarEvent.IsCompleted);

            var status = calendarEvent.IsCompleted ? "завершено" : "возвращено в активные";
            TempData["InfoMessage"] = $"Занятие \"{calendarEvent.Title}\" {status}";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var classes = (await _classRepository.GetByTeacherIdAsync(currentUser.Id))
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();

            var allStudents = await _studentRepository.GetAllWithUserAsync();
            var teacherClasses = await _classRepository.GetByTeacherIdAsync(currentUser.Id);
            var teacherClassIds = teacherClasses.Select(c => c.Id).ToHashSet();
            
            var students = allStudents
                .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                .OrderBy(s => s.User?.LastName ?? "")
                .ToList();

            ViewBag.Classes = new SelectList(classes, "Id", "Name");
            ViewBag.Students = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    Name = $"{s.User?.FullName ?? "Unknown"} ({teacherClasses.FirstOrDefault(c => c.Id == s.ClassId)?.Name ?? "Unknown"})"
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
                new { Value = "#007bff", Text = "Blue", Class = "primary" },
                new { Value = "#28a745", Text = "Green", Class = "success" },
                new { Value = "#dc3545", Text = "Red", Class = "danger" },
                new { Value = "#6c757d", Text = "Grey", Class = "secondary" }
            };
        }
    }
}
