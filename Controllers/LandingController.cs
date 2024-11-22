using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class LandingController : Controller
    {
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _firestoreDb;

        public LandingController(FirestoreDb firestoreDb, FirestoreService firestoreService)
        {
            _firestoreDb = firestoreDb;
            _firestoreService = firestoreService;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch a random testimonial
            var randomTestimonial = await _firestoreService.GetRandomTestimonialAsync();

            // Pass the testimonial to the view
            return View(randomTestimonial);
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

        public IActionResult TestimonailCreatePage()
        {
            if (HttpContext.Session.GetString("UserType") == null)
            {
                TempData["Message"] = "You need to log in to submit a testimonial.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                return RedirectToAction("Create", "Testimonial");
            }
        }

        // --- Payment Page
        public async Task<IActionResult> DonateNow()
        {
            if (HttpContext.Session.GetString("UserType") == null)
            {
                TempData["Message"] = "You need to log in to create a donation.";
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("UserType") != "Donor")
            {
                TempData["Message"] = "You need to login as a Donor to create a donation.";
                return RedirectToAction("Login", "Account");
            }

            // Query Firestore to get the first project name with "Funding Approved" status
            string selectedProjectName = null;
            try
            {
                var companiesCollection = _firestoreDb.Collection("companies");
                var companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                foreach (var companyDoc in companiesSnapshot.Documents)
                {
                    var projectsCollection = companiesCollection.Document(companyDoc.Id).Collection("projects");
                    var projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                    foreach (var projectDoc in projectsSnapshot.Documents)
                    {
                        if (projectDoc.TryGetValue("ProjectName", out string projectName) &&
                            projectDoc.TryGetValue("Status", out string status) &&
                            status == "Funding Approved")
                        {
                            selectedProjectName = projectName;
                            break; // Stop after finding the first project
                        }
                    }

                    if (!string.IsNullOrEmpty(selectedProjectName))
                        break; // Stop iterating through companies if a project is found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching projects: {ex.Message}");
                TempData["Message"] = "Error fetching projects. Please try again later.";
                return RedirectToAction("Index", "Landing");
            }

            // Pass the project name to the Payment page
            return RedirectToAction("Index", "Payment", new { selectedProject = selectedProjectName });
        }
    }
}
