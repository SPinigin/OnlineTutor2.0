using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using System.Diagnostics;

namespace OnlineTutor2.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITeacherRepository _teacherRepository;

        public HomeController(UserManager<ApplicationUser> userManager, 
            ITeacherRepository teacherRepository)
        {
            _userManager = userManager;
            _teacherRepository = teacherRepository;
        }

        public async Task<IActionResult> Index()
        {
            // ��������������� �������������� ������������� � �� ��������
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (User.IsInRole(ApplicationRoles.Teacher))
                    {
                        // ���������, ������� �� �������
                        var teacher = await _teacherRepository.GetByUserIdAsync(user.Id);
                        if (teacher != null && teacher.IsApproved)
                        {
                            return RedirectToAction("Index", "Teacher");
                        }
                        else
                        {
                            TempData["InfoMessage"] = "��� ������� ������� ������� ��������� ���������������.";
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
        public IActionResult Error(int? statusCode = null)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode,
                Path = HttpContext.Request.Path
            };

            // Получаем информацию об исключении из контекста
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature != null)
            {
                var exception = exceptionHandlerPathFeature.Error;
                errorViewModel.Message = exception.Message;
                
                // Логируем только если это не 404
                if (statusCode != 404)
                {
                    // Логирование уже выполнено в middleware
                }
            }

            // Если статус код не передан, пытаемся получить из Response
            if (statusCode == null && Response.StatusCode != 200)
            {
                errorViewModel.StatusCode = Response.StatusCode;
            }

            Response.StatusCode = errorViewModel.StatusCode ?? 500;
            return View(errorViewModel);
        }
    }
}
