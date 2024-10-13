using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers.Company
{
    public class FundingController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<FundingController> _logger;

        public FundingController(FirestoreDb firestoreDb, ILogger<FundingController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        // GET: FundingRequest/Create
        public async Task<IActionResult> Create()
        {
            var companyId = HttpContext.Session.GetString("CompanyId");
            if (string.IsNullOrEmpty(companyId))
            {
                _logger.LogWarning("No CompanyId found in session.");
                return RedirectToAction("Dashboard", "Dashboard"); ;
            }

            var projectsSnapshot = await _firestoreDb.Collection("companies")
                .Document(companyId).Collection("projects")
                .WhereEqualTo("Status", "Project Approved")
                .GetSnapshotAsync();

            var projects = projectsSnapshot.Documents
                .Select(doc => new { Id = doc.Id, Name = doc.GetValue<string>("ProjectName") })
                .ToList();

            ViewBag.Projects = projects;

            return View("~/Views/CompanyServices/CreateFunding.cshtml");
        }

        // POST: FundingRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FundingRequest fundingRequest)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var companyId = HttpContext.Session.GetString("CompanyId");

                    if (string.IsNullOrEmpty(companyId))
                    {
                        _logger.LogWarning("No CompanyId found in session.");
                        return RedirectToAction("Dashboard", "Dashboard");
                    }

                    var fundingRequestRef = _firestoreDb.Collection("companies")
                        .Document(companyId).Collection("fundingRequests").Document();

                    await fundingRequestRef.SetAsync(new
                    {
                        fundingRequest.ProjectId,
                        fundingRequest.Amount,
                        fundingRequest.Motivation,
                    });

                    _logger.LogInformation("Funding request created successfully for ProjectId: {ProjectId}", fundingRequest.ProjectId);

                    var projectRef = _firestoreDb.Collection("companies")
                        .Document(companyId).Collection("projects").Document(fundingRequest.ProjectId);

                    var updateData = new Dictionary<string, object>
                    {
                        { "Status", "Pending Funding" }
                    };

                    await projectRef.UpdateAsync(updateData);

                    _logger.LogInformation("Project status updated to 'Pending Funding' for ProjectId: {ProjectId}", fundingRequest.ProjectId);

                    return RedirectToAction("Dashboard", "Dashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating funding request.");
                    ModelState.AddModelError(string.Empty, "An error occurred while submitting the funding request.");
                }
            }
            else
            {
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error: {Key} - {Error}", modelState.Key, error.ErrorMessage);
                    }
                }
            }

            return View("~/Views/CompanyServices/CreateFunding.cshtml", fundingRequest);
        }
    }
}
