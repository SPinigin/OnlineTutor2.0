using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Models;

namespace OnlineTutor2.ViewComponents
{
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserInfoViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
                return View(user);
            }

            return View();
        }
    }
}
