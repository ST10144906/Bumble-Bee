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

        // Button Actions
        public IActionResult LearnMore()
        {
            // Redirects to the mission section on the landing page
            return RedirectToAction("Index", "Landing", new { section = "mission" });
        }

        // --- Payment Page
        public IActionResult DonateNow()
        {
            return RedirectToAction("Index", "Payment");
        }
    }
}
