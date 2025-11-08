using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Попытка входа пользователя: {Email}", model.Email);

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        user.LastLoginAt = DateTime.Now;
                        await _userManager.UpdateAsync(user);

                        // Проверяем, если это учитель, одобрен ли он
                        if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Teacher))
                        {
                            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                            if (teacher != null && !teacher.IsApproved)
                            {
                                await _signInManager.SignOutAsync();
                                _logger.LogWarning("Попытка входа неодобренного учителя. UserId: {UserId}, Email: {Email}", user.Id, model.Email);
                                TempData["ErrorMessage"] = "Ваш аккаунт учителя еще не одобрен администратором.";
                                return View(model);
                            }
                        }

                        _logger.LogInformation("Успешный вход пользователя. UserId: {UserId}, Email: {Email}", user.Id, model.Email);
                        return RedirectToLocal(returnUrl);
                    }
                }

                _logger.LogWarning("Неудачная попытка входа. Email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            }
            else
            {
                _logger.LogWarning("Попытка входа с невалидными данными. Email: {Email}", model.Email);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Попытка регистрации пользователя. Email: {Email}, Role: {Role}", model.Email, model.Role);

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Добавляем роль
                    await _userManager.AddToRoleAsync(user, model.Role);
                    _logger.LogInformation("Роль {Role} назначена пользователю {UserId}", model.Role, user.Id);

                    try
                    {
                        // Создаем профиль в зависимости от роли
                        if (model.Role == ApplicationRoles.Student)
                        {
                            var student = new Student
                            {
                                UserId = user.Id,
                                School = model.School,
                                Grade = model.Grade
                            };
                            _context.Students.Add(student);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Профиль студента создан. UserId: {UserId}", user.Id);

                            await _signInManager.SignInAsync(user, isPersistent: false);
                            TempData["SuccessMessage"] = "Регистрация прошла успешно! Добро пожаловать!";
                            return RedirectToAction("Index", "Student");
                        }
                        else if (model.Role == ApplicationRoles.Teacher)
                        {
                            var teacher = new Teacher
                            {
                                UserId = user.Id,
                                Subject = model.Subject,
                                Education = model.Education,
                                Experience = model.Experience,
                                IsApproved = false // Требует модерации
                            };
                            _context.Teachers.Add(teacher);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Профиль учителя создан. UserId: {UserId}", user.Id);

                            TempData["InfoMessage"] = "Ваш аккаунт учителя создан, но требует одобрения администратора. Вы получите уведомление на email после проверки.";
                            return RedirectToAction("Login");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при создании профиля пользователя. UserId: {UserId}, Role: {Role}", user.Id, model.Role);
                        await _userManager.DeleteAsync(user);
                        _logger.LogInformation("Пользователь удален из-за ошибки создания профиля. UserId: {UserId}", user.Id);
                        ModelState.AddModelError(string.Empty, "Произошла ошибка при создании профиля. Попробуйте еще раз.");
                        return View(model);
                    }
                }

                _logger.LogWarning("Не удалось создать пользователя. Email: {Email}, Errors: {@Errors}", model.Email, result.Errors);

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                _logger.LogWarning("Попытка регистрации с невалидными данными. Email: {Email}", model.Email);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["InfoMessage"] = "Вы успешно вышли из системы.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Невозможно загрузить пользователя с ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Пользователь успешно изменил пароль.");

            TempData["SuccessMessage"] = "Пароль успешно изменен!";
            return RedirectToAction(nameof(ChangePassword));
        }

        // GET: Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Невозможно загрузить пользователя с ID '{_userManager.GetUserId(User)}'.");
            }

            var viewModel = new ProfileViewModel
            {
                User = user
            };

            // Статистика для студентов
            if (User.IsInRole("Student"))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student != null)
                {
                    // Подсчитываем все пройденные тесты
                    var spellingTests = await _context.SpellingTestResults
                        .Where(r => r.StudentId == student.Id && r.CompletedAt.HasValue)
                        .ToListAsync();

                    var punctuationTests = await _context.PunctuationTestResults
                        .Where(r => r.StudentId == student.Id && r.CompletedAt.HasValue)
                        .ToListAsync();

                    var orthoeopyTests = await _context.OrthoeopyTestResults
                        .Where(r => r.StudentId == student.Id && r.CompletedAt.HasValue)
                        .ToListAsync();

                    var regularTests = await _context.RegularTestResults
                        .Where(r => r.StudentId == student.Id && r.CompletedAt.HasValue)
                        .ToListAsync();

                    // Общее количество тестов
                    viewModel.TotalTestsCompleted = spellingTests.Count +
                                                   punctuationTests.Count +
                                                   orthoeopyTests.Count +
                                                   regularTests.Count;

                    // Общая сумма баллов
                    viewModel.TotalPointsEarned = spellingTests.Sum(r => r.Score) +
                                                 punctuationTests.Sum(r => r.Score) +
                                                 orthoeopyTests.Sum(r => r.Score) +
                                                 regularTests.Sum(r => r.Score);

                    // Средний балл
                    if (viewModel.TotalTestsCompleted > 0)
                    {
                        var allPercentages = new List<double>();
                        allPercentages.AddRange(spellingTests.Select(r => r.Percentage));
                        allPercentages.AddRange(punctuationTests.Select(r => r.Percentage));
                        allPercentages.AddRange(orthoeopyTests.Select(r => r.Percentage));
                        allPercentages.AddRange(regularTests.Select(r => r.Percentage));

                        viewModel.AverageScore = allPercentages.Average();
                    }
                }
            }

            // Статистика для учителей
            if (User.IsInRole("Teacher"))
            {
                var regularTestsCount = await _context.RegularTests
                    .Where(t => t.TeacherId == user.Id)
                    .CountAsync();

                var spellingTestsCount = await _context.SpellingTests
                    .Where(t => t.TeacherId == user.Id)
                    .CountAsync();

                var punctuationTestsCount = await _context.PunctuationTests
                    .Where(t => t.TeacherId == user.Id)
                    .CountAsync();

                var orthoeopyTestsCount = await _context.OrthoeopyTests
                    .Where(t => t.TeacherId == user.Id)
                    .CountAsync();

                // Сумма всех тестов
                viewModel.TotalTests = regularTestsCount +
                                      spellingTestsCount +
                                      punctuationTestsCount +
                                      orthoeopyTestsCount;

                // Загружаем классы учителя со студентами
                var teacherClasses = await _context.Classes
                    .Include(c => c.Students)
                    .Where(c => c.TeacherId == user.Id)
                    .ToListAsync();

                viewModel.TotalClasses = teacherClasses.Count;

                // Уникальные ученики во всех классах учителя
                var uniqueStudentIds = teacherClasses
                    .SelectMany(c => c.Students)
                    .Select(s => s.Id)
                    .Distinct()
                    .Count();

                viewModel.TotalStudents = uniqueStudentIds;
            }

            return View(viewModel);
        }


        // GET: Account/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                CurrentEmail = user.Email
            };

            return View(model);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Проверка возраста (минимум 7 лет)
            var age = DateTime.Now.Year - model.DateOfBirth.Year;
            if (DateTime.Now.DayOfYear < model.DateOfBirth.DayOfYear)
                age--;

            if (age < 7)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Минимальный возраст - 7 лет");
                return View(model);
            }

            if (age > 120)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Указана некорректная дата рождения");
                return View(model);
            }

            // Обновляем основные данные
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;

            var currentEmail = user.Email;

            if (model.Email != currentEmail)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.CurrentEmail = currentEmail;
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.CurrentEmail = currentEmail;
                    return View(model);
                }

                user.EmailConfirmed = false;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.CurrentEmail = currentEmail;
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Пользователь успешно обновил профиль.");

            TempData["SuccessMessage"] = "Профиль успешно обновлен!";
            return RedirectToAction(nameof(Profile));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
