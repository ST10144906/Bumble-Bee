using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers.Company
{
    public class ProjectController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly StorageService _storageService;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(FirestoreDb firestoreDb, StorageService storageService, ILogger<ProjectController> logger)
        {
            _firestoreDb = firestoreDb;
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Project/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Navigating to Create Project view.");
            return View("~/Views/CompanyServices/CreateProject.cshtml");
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, IFormFile miscellaneousDocuments)
        {
            _logger.LogInformation("Entering Create action for Project.");
            project.MiscellaneousDocumentsUrl = string.Empty;
            project.Status = "Pending Approval";
            project.DateCreated = DateTime.UtcNow;
            if (ModelState.IsValid)
            {
                try
                {
                    var companyId = HttpContext.Session.GetString("CompanyId");

                    if (string.IsNullOrEmpty(companyId))
                    {
                        _logger.LogWarning("CompanyId is not set in session. Redirecting to error page.");
                        ModelState.AddModelError(string.Empty, "No company found for the current user. Please ensure your company is created and try again.");
                        return View("~/Views/CompanyServices/CreateProject.cshtml", project);
                    }

                    // Check if the project name already exists
                    if (await IsProjectNameValid(companyId, project.ProjectName))
                    {
                        ModelState.AddModelError(string.Empty, "A project with this name already exists. Please choose a different name.");
                        return View("~/Views/CompanyServices/CreateProject.cshtml", project);
                    }

                    DocumentReference projectDocRef = _firestoreDb.Collection("companies").Document(companyId).Collection("projects").Document();
                    await projectDocRef.SetAsync(new
                    {
                        project.ProjectName,
                        project.Description,
                        project.Status,
                        project.DateCreated,
                        CompanyId = companyId
                    });

                    _logger.LogInformation("Project data saved to Firestore with Document ID: {DocumentId}", projectDocRef.Id);

                    if (miscellaneousDocuments != null && miscellaneousDocuments.Length > 0)
                    {
                        var fileName = $"project-documents/{project.ProjectName}/{miscellaneousDocuments.FileName}";

                        using (var stream = miscellaneousDocuments.OpenReadStream())
                        {
                            project.MiscellaneousDocumentsUrl = await _storageService.UploadFileAsync(stream, fileName);
                        }
                    }

                    if (!string.IsNullOrEmpty(project.MiscellaneousDocumentsUrl))
                    {
                        var updates = new Dictionary<string, object>
                        {
                            { "MiscellaneousDocumentsUrl", project.MiscellaneousDocumentsUrl }
                        };

                        await projectDocRef.UpdateAsync(updates);
                        _logger.LogInformation("Updated Firestore with MiscellaneousDocumentsUrl: {MiscellaneousDocumentsUrl}", project.MiscellaneousDocumentsUrl);
                    }

                    _logger.LogInformation("Project creation process completed successfully.");
                    return RedirectToAction("Dashboard", "Dashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating project.");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the project.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Project creation failed.");

                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            _logger.LogWarning("ModelState Error for {Key}: {ErrorMessage}", key, error.ErrorMessage);
                        }
                    }
                }
            }

            return View("~/Views/CompanyServices/CreateProject.cshtml", project);
        }

        private async Task<bool> IsProjectNameValid(string companyId, string projectName)
        {
            _logger.LogInformation("Validating project name: {ProjectName} for company ID: {CompanyId}", projectName, companyId);

            var projectsRef = _firestoreDb.Collection("companies").Document(companyId).Collection("projects");
            var query = projectsRef.WhereEqualTo("ProjectName", projectName);
            var querySnapshot = await query.GetSnapshotAsync();

            if (querySnapshot.Documents.Any())
            {
                _logger.LogWarning("Project name: {ProjectName} already exists for company ID: {CompanyId}", projectName, companyId);
                return true;
            }

            return false;
        }
    }
}
