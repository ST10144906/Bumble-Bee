using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class AccountController : Controller
    {
        // Navigation
        public IActionResult Login()
        {
            return View();  
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult RegisterDonor()
        {
            return View();
        }

        public IActionResult RegisterCompany()
        {
            return View();
        }
    }
}
