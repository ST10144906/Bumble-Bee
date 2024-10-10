using Microsoft.AspNetCore.Mvc;
using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
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

            // Initialize MiscellaneousDocumentsUrl
            project.MiscellaneousDocumentsUrl = string.Empty;

            if (ModelState.IsValid)
            {
                try
                {
                    project.Status = "Pending";

                    // Hardcoded values for testing
                    string companyId = "TestCompanyId";
                    string taxNumber = "TestTaxNumber";

                    _logger.LogInformation("CompanyId set to: {CompanyId}", companyId);
                    _logger.LogInformation("TaxNumber set to: {TaxNumber}", taxNumber);

                    // Firestore write operation
                    DocumentReference docRef = _firestoreDb.Collection("projects").Document();
                    await docRef.SetAsync(new
                    {
                        ProjectName = project.ProjectName,
                        Description = project.Description,
                        FundingTarget = project.FundingTarget,
                        Status = project.Status, // Ensure this uses the project's status
                        CompanyId = companyId,
                        CompanyTaxID = taxNumber
                    });

                    _logger.LogInformation("Project data saved to Firestore with Document ID: {DocumentId}", docRef.Id);

                    // Handle file upload
                    if (miscellaneousDocuments != null && miscellaneousDocuments.Length > 0)
                    {
                        var fileName = $"project-documents/{project.ProjectName}/{miscellaneousDocuments.FileName}";

                        _logger.LogInformation("Uploading file: {FileName} with size: {FileSize} bytes", fileName, miscellaneousDocuments.Length);

                        using (var stream = miscellaneousDocuments.OpenReadStream())
                        {
                            // Upload file and get the URL
                            project.MiscellaneousDocumentsUrl = await _storageService.UploadFileAsync(stream, fileName);
                        }

                        _logger.LogInformation("MiscellaneousDocumentsUrl after upload: {MiscellaneousDocumentsUrl}", project.MiscellaneousDocumentsUrl);
                    }
                    else
                    {
                        _logger.LogWarning("No miscellaneous documents uploaded.");
                    }

                    // Update Firestore only if URL is not empty
                    if (!string.IsNullOrEmpty(project.MiscellaneousDocumentsUrl))
                    {
                        var updates = new Dictionary<string, object>
                {
                    { "MiscellaneousDocumentsUrl", project.MiscellaneousDocumentsUrl }
                };

                        await docRef.UpdateAsync(updates);
                        _logger.LogInformation("Updated Firestore with MiscellaneousDocumentsUrl: {MiscellaneousDocumentsUrl}", project.MiscellaneousDocumentsUrl);
                    }
                    else
                    {
                        _logger.LogWarning("MiscellaneousDocumentsUrl is empty, not updating Firestore.");
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
                // Log ModelState errors
                _logger.LogWarning("ModelState is invalid. Project creation failed.");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Validation error in '{Key}': {ErrorMessage}", key, error.ErrorMessage);
                    }
                }
            }

            // Return the view with the model to display validation errors or other issues
            return View("~/Views/CompanyServices/CreateProject.cshtml", project);
        }
    }
}
