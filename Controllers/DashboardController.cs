using BumbleBeeWebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            string role = GetUserRole(User.Identity.Name);

            var model = new DashboardViewModel
            {
                UserRole = role
            };

            return View("Dashboard", model);
        }

        private string GetUserRole(string username)
        {
            // Placeholder for getting user role, replace with actual logic
            return "Admin"; // Default to Admin for testing
        }
    }
}

