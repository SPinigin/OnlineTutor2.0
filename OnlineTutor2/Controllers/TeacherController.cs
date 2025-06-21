using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Models;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TeacherController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Кабинет учителя";
            return View();
        }
    }
}
