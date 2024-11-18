using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AuthService _authService;
        private readonly FirestoreService _firestoreService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(FirestoreService firestoreService, ILogger<DashboardController> logger)
        {
            _authService = new AuthService(firestoreService);
            _firestoreService = firestoreService;
            _logger = logger;
        }

        // GET: Dashboard
        public async Task<IActionResult> Dashboard()
        {
            string userId = HttpContext.Session.GetString("UserId");
            bool isPartOfCompany = false;
            string companyId = null;

            var companyDocument = await _authService.GetCompanyDocumentByUserIdAsync(userId);
            if (companyDocument != null)
            {
                isPartOfCompany = true;
                companyId = companyDocument.Id;
                HttpContext.Session.SetString("CompanyId", companyId);
            }

            var userDocument = await _authService.GetUserDocumentAsync(userId);
            var userData = userDocument.ConvertTo<Dictionary<string, object>>();

            // Load projects if the user is a donor
            List<CompanyProjectsViewModel> companyProjects = new List<CompanyProjectsViewModel>();
            if (HttpContext.Session.GetString("UserType") == "Donor")
            {
                companyProjects = await LoadDonorProjects();
            }

            // Pass userData and companyProjects to the view
            ViewBag.UserData = userData;
            return View(companyProjects); // Return the populated companyProjects list
        }

        private async Task<List<CompanyProjectsViewModel>> LoadDonorProjects()
        {
            List<CompanyProjectsViewModel> companyProjects = new List<CompanyProjectsViewModel>();

            try
            {
                var companySnapshots = await _firestoreService.GetCollectionAsync("companies");

                foreach (var companySnapshot in companySnapshots.Documents)
                {
                    var companyName = companySnapshot.GetValue<string>("Name");
                    var companyDescription = companySnapshot.GetValue<string>("Description");

                    var companyProjectsViewModel = new CompanyProjectsViewModel
                    {
                        CompanyName = companyName,
                        CompanyDescription = companyDescription,
                        Projects = new List<Project>()
                    };

                    var projectSnapshots = await companySnapshot.Reference.Collection("projects").GetSnapshotAsync();

                    foreach (var projectSnapshot in projectSnapshots.Documents)
                    {
                        // Convert project document to the Project model
                        var project = projectSnapshot.ConvertTo<Project>();

                        // Ensure project has all necessary properties set
                        if (project != null)
                        {
                            companyProjectsViewModel.Projects.Add(project);
                        }
                    }

                    companyProjects.Add(companyProjectsViewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching projects from Firestore.");
                ModelState.AddModelError(string.Empty, "Error with servers right now.");
            }

            if (companyProjects.Count == 0)
            {
                ViewBag.NoProjectsMessage = "No projects found.";
            }

            return companyProjects;
        }

    }
}
