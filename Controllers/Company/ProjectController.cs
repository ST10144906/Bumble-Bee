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

            if (ModelState.IsValid)
            {
                try
                {
                    var uid = HttpContext.Session.GetString("Uid");

                    var companyQuery = _firestoreDb.Collection("companies").WhereEqualTo("UID", uid);
                    var companySnapshot = await companyQuery.GetSnapshotAsync();

                    if (companySnapshot.Documents.Count == 0)
                    {
                        ModelState.AddModelError(string.Empty, "No company found for the current user.");
                        return View("~/Views/CompanyServices/CreateProject.cshtml", project);
                    }

                    var companyId = companySnapshot.Documents[0].Id; // Get the first company's ID

                    project.Status = "Pending";

                    DocumentReference projectDocRef = _firestoreDb.Collection("companies").Document(companyId).Collection("projects").Document();
                    await projectDocRef.SetAsync(new
                    {
                        project.ProjectName,
                        project.Description,
                        project.FundingTarget,
                        project.Status,
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
                    return RedirectToAction("Index");
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
            }

            return View("~/Views/CompanyServices/CreateProject.cshtml", project);
        }
    }
}
