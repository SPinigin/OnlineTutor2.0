using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using System.Diagnostics;

namespace OnlineTutor2.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Перенаправление авторизованных пользователей в их кабинеты
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (User.IsInRole(ApplicationRoles.Teacher))
                    {
                        // Проверяем, одобрен ли учитель
                        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                        if (teacher != null && teacher.IsApproved)
                        {
                            return RedirectToAction("Index", "Teacher");
                        }
                        else
                        {
                            TempData["InfoMessage"] = "Ваш аккаунт учителя ожидает одобрения администратором.";
                        }
                    }
                    else if (User.IsInRole(ApplicationRoles.Student))
                    {
                        return RedirectToAction("Index", "Student");
                    }
                    else if (User.IsInRole(ApplicationRoles.Admin))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
