using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        // Navbar Methods
        public IActionResult LoginPage()
        {
            return RedirectToAction("Login", "Account");
        }

        public IActionResult RegisterPage()
        {
            return RedirectToAction("Register", "Account");
        }
    }
}
