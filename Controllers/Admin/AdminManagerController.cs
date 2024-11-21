using BumbleBeeWebApp.Models;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

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
                        UID = document.Id,
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

        public async Task<IActionResult> Delete(string documentUid, string deleteEmail)
        {
            try
            {
                // Fetch user data from Firestore to get the email
                DocumentSnapshot documentSnapshot = await _firestoreService.GetDocumentAsync("users", documentUid);
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(deleteEmail);

                var authId = userRecord.Uid;
                Console.WriteLine(documentUid);
                Console.WriteLine(authId);

                if (documentSnapshot.Exists)
                {
                    // Get email from the Firestore document
                    Dictionary<string, object> userData = documentSnapshot.ToDictionary();
                    string email = userData.ContainsKey("Email") ? userData["Email"]?.ToString() : null;

                    if (string.IsNullOrEmpty(email))
                    {
                        TempData["ErrorMessage"] = "User email not found.";
                        return await LoadUsers();
                    }

                    // Step 1: Delete the user from Firestore
                    await _firestoreService.DeleteUserFromFirestoreAsync(documentUid);

                    // Step 2: Delete the user from Firebase Authentication
                    await FirebaseAuth.DefaultInstance.DeleteUserAsync(authId);

                    TempData["SuccessMessage"] = "User successfully deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "User not found in Firestore.";
                }
            }
            catch (FirebaseAuthException ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user from FirebaseAuth: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }

            return await LoadUsers(); // Redirect to your user listing page
        }
    }
}
