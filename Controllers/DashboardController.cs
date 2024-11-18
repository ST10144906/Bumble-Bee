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
        private readonly FirestoreDb _firestoreDb;
        private readonly FirestoreService _firestoreService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(FirestoreService firestoreService, FirestoreDb firestoreDb, ILogger<DashboardController> logger)
        {
            _authService = new AuthService(firestoreService);
            _firestoreService = firestoreService;
            _firestoreDb = firestoreDb;
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

            // Load projects if the user is a or admin

            /* if (HttpContext.Session.GetString("UserType") == "Donor")
            {
                companyProjects = await LoadDonorProjects();
            } */

            var allProjects = await LoadAllProjects();
            // Pass userData and companyProjects to the view
            ViewBag.UserData = userData;
            return View(allProjects); // Return the populated companyProjects list
        }

        private async Task<List<Project>> LoadAllProjects()
        {
            List<Project> allProjects = new List<Project>();

            try
            {
                // Retrieve all documents from the "companies" collection
                CollectionReference companiesCollection = _firestoreDb.Collection("companies");
                QuerySnapshot companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                foreach (DocumentSnapshot companyDoc in companiesSnapshot.Documents)
                {
                    if (companyDoc.Exists)
                    {
                        // Get the "projects" subcollection for each company
                        CollectionReference projectsCollection = companyDoc.Reference.Collection("projects");
                        QuerySnapshot projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                        foreach (DocumentSnapshot projectDoc in projectsSnapshot.Documents)
                        {
                            if (projectDoc.Exists)
                            {
                                try
                                {
                                    string status = projectDoc.ContainsField("Status") ? projectDoc.GetValue<string>("Status") : null;

                                    if (status == "Funding Approved")
                                    {
                                        var project = new Project
                                        {
                                            Id = projectDoc.Id,  // Store Firestore document ID
                                            ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                            Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                            DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                            MiscellaneousDocumentsUrl = projectDoc.ContainsField("MiscellaneousDocumentsUrl") ? projectDoc.GetValue<string>("MiscellaneousDocumentsUrl") : null
                                        };

                                        allProjects.Add(project);
                                    }
                                }
                                catch (Exception innerEx)
                                {
                                    _logger.LogError(innerEx, $"Error processing project document ID: {projectDoc.Id}");
                                }
                            }
                        }
                    }
                }

                // Sort projects by DateCreated (most recent first)
                allProjects = allProjects.OrderByDescending(p => p.DateCreated).ToList();

                if (!allProjects.Any())
                {
                    _logger.LogInformation("No donor projects found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donor projects from Firestore.");
            }

            return allProjects;
        }


        // Fetch project details
        public async Task<IActionResult> ProjectDetails(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                return NotFound();
            }

            var project = await LoadProjectById(projectId);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        private async Task<Project> LoadProjectById(string projectId)
        {
            try
            {
                // Retrieve all documents from the "companies" collection
                CollectionReference companiesCollection = _firestoreDb.Collection("companies");
                QuerySnapshot companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                foreach (DocumentSnapshot companyDoc in companiesSnapshot.Documents)
                {
                    if (companyDoc.Exists)
                    {
                        // Get the "projects" subcollection for each company
                        CollectionReference projectsCollection = companyDoc.Reference.Collection("projects");
                        QuerySnapshot projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                        foreach (DocumentSnapshot projectDoc in projectsSnapshot.Documents)
                        {
                            if (projectDoc.Exists && projectDoc.Id == projectId) // Match project by ID
                            {
                                return new Project
                                {
                                    Id = projectDoc.Id,  // Capture the Firestore document ID
                                    ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                    Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                    DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                    MiscellaneousDocumentsUrl = projectDoc.ContainsField("MiscellaneousDocumentsUrl") ? projectDoc.GetValue<string>("MiscellaneousDocumentsUrl") : null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project details from Firestore.");
            }

            return null; // Return null if no project was found
        }
    }
}
