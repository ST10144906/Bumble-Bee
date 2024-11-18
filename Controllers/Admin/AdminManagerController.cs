using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace BumbleBeeWebApp.Controllers.Admin
{
    public class AdminManagerController : Controller
    {
        private readonly AuthService _authService;
        private readonly FirestoreService _firestoreService;
        private readonly ILogger<DashboardController> _logger;

        public AdminManagerController(FirestoreService firestoreService, ILogger<DashboardController> logger)
        {
            _authService = new AuthService(firestoreService);
            _firestoreService = firestoreService;
            _logger = logger;
        }

        // GET : Gets all Users
        [HttpGet]
        public async Task<IActionResult> LoadUsers()
        {
            List<User> users = new List<User>();

            // Get the collection of users from Firestore
            QuerySnapshot userQuerySnapshot = await _firestoreService.GetCollectionAsync("users");

            foreach (DocumentSnapshot document in userQuerySnapshot.Documents)
            {
                if (document.Exists)
                {
                    Dictionary<string, object> userData = document.ToDictionary();
                    User user = new User
                    {
                        FullName = userData.ContainsKey("FullName") ? userData["FullName"]?.ToString() : string.Empty,
                        Email = userData.ContainsKey("Email") ? userData["Email"]?.ToString() : string.Empty,
                        Role = userData.ContainsKey("Type") ? userData["Type"]?.ToString() : string.Empty
                    };

                    // Filter users with the role "Admin"
                    if (user.Role == "Admin")
                    {
                        users.Add(user);
                    }
                }
            }

            return View("~/Views/Admin/AdminManager.cshtml", users);
        }

        public async Task<IActionResult> DeleteAdmin(string id)
        {
            return null;
        }

    }
}
