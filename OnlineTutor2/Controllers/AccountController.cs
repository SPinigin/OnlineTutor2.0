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

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
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
                                TempData["ErrorMessage"] = "Ваш аккаунт учителя еще не одобрен администратором.";
                                return View(model);
                            }
                        }

                        return RedirectToLocal(returnUrl);
                    }
                }

                ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
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

                            TempData["InfoMessage"] = "Ваш аккаунт учителя создан, но требует одобрения администратора. Вы получите уведомление на email после проверки.";
                            return RedirectToAction("Login");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Если произошла ошибка при создании профиля, удаляем пользователя
                        await _userManager.DeleteAsync(user);
                        ModelState.AddModelError(string.Empty, "Произошла ошибка при создании профиля. Попробуйте еще раз.");
                        Console.WriteLine(ex);
                        return View(model);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
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
