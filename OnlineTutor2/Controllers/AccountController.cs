using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        #region Login/Logout

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Попытка входа пользователя: {Email}", model.Email);

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Проверка подтверждения email
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        TempData["WarningMessage"] = "Пожалуйста, подтвердите ваш email перед входом. Проверьте свою почту или запросите новое письмо.";
                        return RedirectToAction(nameof(ResendEmailConfirmation));
                    }

                    // Проверка активности аккаунта
                    if (!user.IsActive)
                    {
                        _logger.LogWarning("Попытка входа заблокированного пользователя. UserId: {UserId}", user.Id);
                        ModelState.AddModelError(string.Empty, "Ваш аккаунт заблокирован. Обратитесь к администратору.");
                        return View(model);
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
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

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Аккаунт {Email} заблокирован из-за множественных неудачных попыток входа", model.Email);
                    TempData["ErrorMessage"] = "Ваш аккаунт временно заблокирован из-за множественных неудачных попыток входа. Попробуйте позже.";
                    return View(model);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Пользователь вышел из системы");
            TempData["InfoMessage"] = "Вы успешно вышли из системы.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Register

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
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
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
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
                                Grade = model.Grade,
                                CreatedAt = DateTime.Now
                            };
                            _context.Students.Add(student);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Профиль студента создан. UserId: {UserId}", user.Id);
                        }
                        else if (model.Role == ApplicationRoles.Teacher)
                        {
                            var teacher = new Teacher
                            {
                                UserId = user.Id,
                                Subject = model.Subject,
                                Education = model.Education,
                                Experience = model.Experience,
                                IsApproved = false,
                                CreatedAt = DateTime.Now
                            };
                            _context.Teachers.Add(teacher);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Профиль учителя создан. UserId: {UserId}", user.Id);
                        }

                        // Генерируем токен подтверждения email
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                        // Создаем ссылку подтверждения
                        var callbackUrl = Url.Action(
                            "ConfirmEmail",
                            "Account",
                            new { userId = user.Id, code = code },
                            protocol: Request.Scheme);

                        // Отправляем email с подтверждением
                        var emailBody = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <div style='background-color: #0d6efd; padding: 20px; text-align: center;'>
                                    <h1 style='color: white; margin: 0;'>
                                        🎓 Добро пожаловать в Тьюторную!
                                    </h1>
                                </div>
                                <div style='padding: 30px; background-color: #f8f9fa;'>
                                    <h2>Здравствуйте, {user.FirstName}!</h2>
                                    <p>Спасибо за регистрацию на нашей образовательной платформе <strong>Тьюторная</strong>.</p>
                                    {(model.Role == ApplicationRoles.Teacher ?
                                        "<p><strong>Обратите внимание:</strong> Ваш аккаунт учителя требует одобрения администратора. После подтверждения email и одобрения администратором вы получите доступ ко всем функциям платформы.</p>" :
                                        "<p>Для завершения регистрации и активации вашего аккаунта, пожалуйста, подтвердите ваш email-адрес:</p>")}
                                    <div style='text-align: center; margin: 30px 0;'>
                                        <a href='{callbackUrl}' 
                                           style='padding: 15px 30px; 
                                                  background-color: #198754; 
                                                  color: white; 
                                                  text-decoration: none; 
                                                  border-radius: 5px; 
                                                  display: inline-block;
                                                  font-weight: bold;'>
                                            ✓ Подтвердить email
                                        </a>
                                    </div>
                                    <p style='color: #666; font-size: 14px;'>
                                        Если кнопка не работает, скопируйте и вставьте эту ссылку в браузер:<br>
                                        <a href='{callbackUrl}'>{callbackUrl}</a>
                                    </p>
                                    <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                                    <p style='color: #666; font-size: 12px;'>
                                        Если вы не регистрировались на платформе Тьюторная, просто проигнорируйте это письмо.
                                    </p>
                                </div>
                                <div style='background-color: #e9ecef; padding: 15px; text-align: center;'>
                                    <p style='color: #666; font-size: 12px; margin: 0;'>
                                        © 2025 Тьюторная. Образовательная платформа.<br>
                                        По вопросам: <a href='mailto:pn31@mail.ru'>pn31@mail.ru</a>
                                    </p>
                                </div>
                            </div>
                        ";

                        try
                        {
                            await _emailSender.SendEmailAsync(
                                user.Email,
                                "Подтверждение email - Тьюторная",
                                emailBody);

                            _logger.LogInformation($"Письмо подтверждения отправлено на {user.Email}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Ошибка отправки письма подтверждения: {ex.Message}");
                            // Продолжаем регистрацию даже если письмо не отправилось
                        }

                        // Перенаправляем на страницу с информацией о подтверждении
                        return RedirectToAction(nameof(RegisterConfirmation), new { email = user.Email, role = model.Role });
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

        // GET: Account/RegisterConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterConfirmation(string email, string role)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Email = email;
            ViewBag.IsTeacher = role == ApplicationRoles.Teacher;
            return View();
        }

        #endregion

        #region Email Confirmation

        // GET: Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Невозможно загрузить пользователя с ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Email подтвержден для пользователя {user.Email}");

                // Проверяем, является ли пользователь учителем
                var isTeacher = await _userManager.IsInRoleAsync(user, ApplicationRoles.Teacher);
                ViewBag.IsTeacher = isTeacher;

                return View("ConfirmEmail");
            }
            else
            {
                ViewBag.ErrorMessage = "Ошибка подтверждения email. Возможно, ссылка устарела.";
                return View("ConfirmEmail");
            }
        }

        // GET: Account/ResendEmailConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        // POST: Account/ResendEmailConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Не раскрываем, что пользователь не существует
                return RedirectToAction(nameof(ResendEmailConfirmationSuccess));
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                // Email уже подтвержден
                TempData["InfoMessage"] = "Этот email уже подтвержден. Вы можете войти в систему.";
                return RedirectToAction(nameof(Login));
            }

            // Генерируем новый токен
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #0d6efd; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>Подтверждение email</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <h2>Здравствуйте, {user.FirstName}!</h2>
                        <p>Вы запросили повторную отправку ссылки для подтверждения email.</p>
                        <p>Нажмите на кнопку ниже для подтверждения:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' 
                               style='padding: 15px 30px; 
                                      background-color: #198754; 
                                      color: white; 
                                      text-decoration: none; 
                                      border-radius: 5px; 
                                      display: inline-block;
                                      font-weight: bold;'>
                                ✓ Подтвердить email
                            </a>
                        </div>
                    </div>
                </div>
            ";

            try
            {
                await _emailSender.SendEmailAsync(model.Email, "Подтверждение email - Тьюторная", emailBody);
                _logger.LogInformation($"Повторное письмо подтверждения отправлено на {model.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка отправки письма: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка отправки письма. Попробуйте позже.";
                return View(model);
            }

            return RedirectToAction(nameof(ResendEmailConfirmationSuccess));
        }

        // GET: Account/ResendEmailConfirmationSuccess
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmationSuccess()
        {
            return View();
        }

        #endregion

        #region Password Reset

        // GET: Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Не раскрываем, что пользователь не существует или email не подтвержден
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Генерируем токен для сброса пароля
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Создаем ссылку для сброса пароля
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            // Отправляем email
            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #dc3545; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>🔒 Восстановление пароля</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <h2>Здравствуйте, {user.FirstName}!</h2>
                        <p>Вы запросили восстановление пароля для вашего аккаунта на платформе <strong>Тьюторная</strong>.</p>
                        <p>Для сброса пароля перейдите по ссылке:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' 
                               style='padding: 15px 30px; 
                                      background-color: #0d6efd; 
                                      color: white; 
                                      text-decoration: none; 
                                      border-radius: 5px; 
                                      display: inline-block;
                                      font-weight: bold;'>
                                Сбросить пароль
                            </a>
                        </div>
                        <p style='color: #666;'>Если вы не запрашивали восстановление пароля, просто проигнорируйте это письмо.</p>
                        <p style='color: #dc3545; font-weight: bold;'>⏰ Ссылка действительна в течение 24 часов.</p>
                    </div>
                </div>
            ";

            try
            {
                await _emailSender.SendEmailAsync(model.Email, "Восстановление пароля - Тьюторная", emailBody);
                _logger.LogInformation($"Письмо для восстановления пароля отправлено на {model.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка отправки письма: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка отправки письма. Попробуйте позже.";
                return View(model);
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // GET: Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Необходим код для сброса пароля");
            }

            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Не раскрываем, что пользователь не существует
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation($"Пользователь {user.Email} успешно сбросил пароль");
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        #endregion

        #region Profile Management

        // GET: Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
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
        [Authorize]
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
            if (User.IsInRole(ApplicationRoles.Student))
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
            if (User.IsInRole(ApplicationRoles.Teacher))
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
                viewModel.TotalStudents = teacherClasses
                    .SelectMany(c => c.Students)
                    .Select(s => s.Id)
                    .Distinct()
                    .Count();
            }

            return View(viewModel);
        }

        // GET: Account/EditProfile
        [HttpGet]
        [Authorize]
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
        [Authorize]
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

        #endregion

        #region Helpers

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
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

        #endregion
    }
}
