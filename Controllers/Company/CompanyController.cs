using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers.Company
{
    public class CompanyController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly StorageService _storageService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(FirestoreDb firestoreDb, StorageService storageService, ILogger<CompanyController> logger)
        {
            _firestoreDb = firestoreDb;
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Company/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Navigating to Create/Edit Company view.");

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid))
            {
                _logger.LogWarning("User not logged in. Redirecting to login page.");
                return RedirectToAction("Login", "Account");
            }

            // Check if the user already has a company
            var existingCompany = await GetCompanyByUserIdAsync(uid);
            if (existingCompany != null)
            {
                _logger.LogInformation("Existing company found for UID: {UID}", uid);
                return View("~/Views/CompanyServices/CreateCompany.cshtml", existingCompany);
            }

            return View("~/Views/CompanyServices/CreateCompany.cshtml");
        }

        // POST: Company/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Company company, IFormFile? document)
        {
            _logger.LogInformation("Entering Create/Edit action for Company.");

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid))
            {
                ModelState.AddModelError(string.Empty, "User is not logged in. Please log in first.");
                return View("~/Views/CompanyServices/CreateCompany.cshtml", company);
            }

            company.UID = uid;
            company.ApprovalStatus = company.ApprovalStatus ?? "Pending Approval";
            company.DocumentUrl = company.DocumentUrl ?? string.Empty;

            // Remove fields that are not part of the form submission
            ModelState.Remove("UID");
            ModelState.Remove("CompanyID");
            ModelState.Remove("ApprovalStatus");
            ModelState.Remove("document");
            ModelState.Remove("DocumentUrl");


            if (ModelState.IsValid)
            {
                try
                {
                    var companyDocRef = _firestoreDb.Collection("companies").Document(company.CompanyID ?? Guid.NewGuid().ToString());

                    // Upload document if provided
                    if (document != null && document.Length > 0)
                    {
                        var fileName = $"company-documents/{company.Name}/{document.FileName}";
                        using (var stream = document.OpenReadStream())
                        {
                            company.DocumentUrl = await _storageService.UploadFileAsync(stream, fileName);
                        }
                    }

                    // Save or update company details
                    await companyDocRef.SetAsync(new
                    {
                        company.UID,
                        company.Name,
                        company.ReferenceNumber,
                        company.TaxNumber,
                        company.Description,
                        company.Email,
                        company.PhoneNumber,
                        company.ApprovalStatus,
                        company.DocumentUrl
                    }, SetOptions.MergeAll);

                    _logger.LogInformation("Company data saved to Firestore with Document ID: {DocumentId}", companyDocRef.Id);

                    TempData["SuccessMessage"] = "Company details saved successfully!";
                    return RedirectToAction("Dashboard", "Dashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while saving company details.");
                    ModelState.AddModelError(string.Empty, "An error occurred while saving company details.");
                }
            }
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        _logger.LogWarning("ModelState Error for {Key}: {Errors}", state.Key, string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }


            return View("~/Views/CompanyServices/CreateCompany.cshtml", company);
        }

        private async Task<Models.Company> GetCompanyByUserIdAsync(string uid)
        {
            var companiesCollection = _firestoreDb.Collection("companies");
            var companyQuery = companiesCollection.WhereEqualTo("UID", uid);
            var querySnapshot = await companyQuery.GetSnapshotAsync();

            // Use Firestore serialization to convert the document to a Company
            return querySnapshot.Documents
                .Select(doc => doc.ConvertTo<Models.Company>())
                .FirstOrDefault();
        }

    }
}
