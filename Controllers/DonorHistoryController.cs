using BumbleBeeWebApp.Controllers.Company;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers.DonorHistory
{
    public class DonorHistoryController : Controller
    {

        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<CompanyController> _logger;

        public DonorHistoryController(FirestoreDb firestoreDb, ILogger<CompanyController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Navigating to Donor History View.");

            string userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("User is not logged in. Redirecting to login page.");
                return RedirectToAction("Login", "Account");
            }

            // Retrieve donations for the user
            var donations = new List<Dictionary<string, object>>();
            try
            {
                Query donationsQuery = _firestoreDb.Collection("donations").WhereEqualTo("UserEmail", userEmail);
                QuerySnapshot donationsSnapshot = await donationsQuery.GetSnapshotAsync();

                foreach (var document in donationsSnapshot.Documents)
                {
                    donations.Add(document.ToDictionary());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donations for user: {UserEmail}", userEmail);
                TempData["ErrorMessage"] = "An error occurred while retrieving your donation history. Please try again later.";
                return View("~/Views/Dashboard/DonationHistory.cshtml", new List<Dictionary<string, object>>());
            }

            // Check if there are no donations
            if (!donations.Any())
            {
                TempData["NoHistoryMessage"] = "No donation history available.";
            }

            // Pass donations to the view
            return View("~/Views/Dashboard/DonationHistory.cshtml", donations);
        }

        // GET: DonorController/Details/5

    }
}