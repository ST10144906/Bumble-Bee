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

        public async Task<IActionResult> Index(string userId)
        {
            // Fetch user document from Firestore using userId
            var userDoc = await _authService.GetUserDocumentAsync(userId);

            if (userDoc == null)
            {
                return NotFound("User document not found.");
            }

            // Map userDoc to DashboardViewModel
            var model = new DashboardViewModel
            {
                UserRole = userDoc.ContainsField("Type") ? userDoc.GetValue<string>("Type") : "Unknown",
                UserName = userDoc.ContainsField("FullName") ? userDoc.GetValue<string>("FullName") : "Guest",
                UserEmail = userDoc.ContainsField("Email") ? userDoc.GetValue<string>("Email") : "Unknown"
            };

            //--- FOR TESTING PURPOSES REMOVE LATER
            var userDocData = userDoc.ToDictionary();
            foreach (var field in userDocData)
            {
                Console.WriteLine($"{field.Key}: {field.Value}");
            }
            //Console.WriteLine("name: "+ userDoc.GetValue<string>("FullName")  );
            return View("~/Views/Dashboard/Dashboard.cshtml", model);
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

        /*public ActionResult Index()
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
        */
    }
}

