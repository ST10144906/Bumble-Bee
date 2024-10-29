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

        /*
        public async Task<IActionResult> loadProjects()
        {
            List<CompanyProjectsViewModel> companyProjects = new List<CompanyProjectsViewModel>(); // Initialize the list

            try
            {
                var companyCollection = _firestoreDb.Collection("companies");
                var companyDocuments = await companyCollection.GetSnapshotAsync();

                foreach (var companyDocument in companyDocuments.Documents)
                {
                    // Get company details
                    var companyName = companyDocument.GetValue<string>("Name");
                    var companyDescription = companyDocument.GetValue<string>("Description");

                    // Create a new view model to hold company and projects
                    var companyProjectsViewModel = new CompanyProjectsViewModel
                    {
                        CompanyName = companyName,
                        CompanyDescription = companyDescription,
                        Projects = new List<Project>()
                    };

                    // Get the sub-collection of projects for this company
                    var projectCollection = companyDocument.Reference.Collection("projects");
                    var projectDocuments = await projectCollection.GetSnapshotAsync();

                    foreach (var projectDocument in projectDocuments.Documents)
                    {
                        // Convert project document to the Project model and add it to the company's project list
                        var project = projectDocument.ConvertTo<Project>();
                        companyProjectsViewModel.Projects.Add(project);
                    }

                    companyProjects.Add(companyProjectsViewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching projects from Firestore.");
                ModelState.AddModelError(string.Empty, "Error with servers right now.");
            }

            // If no projects are found, set a message to display in the view
            if (companyProjects.Count == 0)
            {
                ViewBag.NoProjectsMessage = "No projects found.";
            }

            // Return the view with the populated companyProjects list
            return View(companyProjects);
        }*/
    }
}
