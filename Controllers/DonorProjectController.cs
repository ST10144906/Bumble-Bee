using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers
{
    public class DonorProjectController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<DonorProjectController> _logger;

        public DonorProjectController(FirestoreDb firestoreDb, ILogger<DonorProjectController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        // GET: DonorProject/ViewAllProjects
        public async Task<IActionResult> ViewAllProjects()
        {
            List<Project> projects = new List<Project>(); // Initialize the list

            try
            {
                var companyCollection = _firestoreDb.Collection("companies");
                var companyDocuments = await companyCollection.GetSnapshotAsync();

                foreach (var companyDocument in companyDocuments.Documents)
                {
                    var projectCollection = companyDocument.Reference.Collection("projects");
                    var projectDocuments = await projectCollection.GetSnapshotAsync();

                    foreach (var projectDocument in projectDocuments.Documents)
                    {
                        var project = projectDocument.ConvertTo<Project>();
                        projects.Add(project);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching projects from Firestore.");
                ModelState.AddModelError(string.Empty, "Error with servers right now.");
            }

            // Check if the projects list is empty and return an appropriate view
            if (projects.Count == 0)
            {
                ViewBag.NoProjectsMessage = "No projects found.";
            }

            return View(projects);
        }
    }
}
