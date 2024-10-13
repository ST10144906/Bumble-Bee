using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers.Company
{
    public class CompanyController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(FirestoreDb firestoreDb, ILogger<CompanyController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        // GET: Company/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Navigating to Create Company view.");
            return View("~/Views/CompanyServices/CreateCompany.cshtml");
        }

        // POST: Company/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Company company)
        {
            _logger.LogInformation("Entering Create action for Company.");
             
            var uid = HttpContext.Session.GetString("UserId");
            _logger.LogInformation("UID: " + uid);

            if (string.IsNullOrEmpty(uid))
            {
                ModelState.AddModelError(string.Empty, "User is not logged in. Please log in first.");
                return View("~/Views/CompanyServices/CreateCompany.cshtml", company);
            }

            var existingCompany = await GetCompanyByUserIdAsync(uid);
            if (existingCompany != null)
            {
                ModelState.AddModelError(string.Empty, "You have already created a company. You cannot create another one.");
                return View("~/Views/CompanyServices/CreateCompany.cshtml", company);
            }

            company.UID = uid;

            ModelState.Remove("UID");

            if (ModelState.IsValid)
            {
                try
                {
                    DocumentReference companyDocRef = _firestoreDb.Collection("companies").Document();
                    await companyDocRef.SetAsync(new
                    {
                        company.UID,
                        company.Name,
                        company.ReferenceNumber,
                        company.TaxNumber,
                        company.Description,
                        company.Email,
                        company.PhoneNumber
                    });

                    _logger.LogInformation("Company data saved to Firestore with Document ID: {DocumentId}", companyDocRef.Id);
                    TempData["SuccessMessage"] = "Company created successfully! You can add projects later.";
                    return RedirectToAction("Dashboard", "Dashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating company.");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the company.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Company creation failed.");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning("ModelState error: {ErrorMessage}", error.ErrorMessage);
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

            return querySnapshot.Documents
                .Select(doc => doc.ConvertTo<Models.Company>())
                .FirstOrDefault();
        }
    }
}
