using BumbleBeeWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AuthService _authService;


        public DashboardController(AuthService authService)
        {
            _authService = authService;
        }

        /*
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity.Name;
            var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(userEmail);
            var userId = userRecord.Uid;

            var userDoc = await _authService.GetUserDocumentAsync(userId);
            string userType = userDoc.ContainsField("Type") ? userDoc.GetValue<string>("Type") : "Unknown";

            var model = new DashboardViewModel
            {
                UserRole = userType
            };

            return View("Dashboard", model);
        }
        */

        public ActionResult Index()
        {
            string role = "Company";

            var model = new DashboardViewModel
            {
                UserRole = role
            };

            return View("Dashboard", model);
        }

        private string GetUserRole(string username)
        {
            // Placeholder for getting user role, replace with actual logic
            return "Company"; // Default to Admin for testing
        }
    }
}

