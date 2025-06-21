using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Models;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Student)]
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Кабинет ученика";
            return View();
        }
    }
}
