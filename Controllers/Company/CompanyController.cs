using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
        // POST: Company/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Company company)
        {
            _logger.LogInformation("Entering Create action for Company.");

            // Get the UID from session
            var uid = HttpContext.Session.GetString("Uid");
            _logger.LogInformation("UID" + uid);
            if (string.IsNullOrEmpty(uid))
            {
                ModelState.AddModelError(string.Empty, "User is not logged in. Please log in first.");
                return View("~/Views/CompanyServices/CreateCompany.cshtml", company);
            }

            // Assign UID from session to the company model
            company.UID = uid;

            // Remove UID from ModelState validation
            ModelState.Remove("UID");

            // Validate model state
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
                    return View("~/Views/Dashboard/Dashboard.cshtml");
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


    }
}
