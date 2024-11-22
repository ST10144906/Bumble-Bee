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

                                    if (HttpContext.Session.GetString("UserType") == "Admin" && status == "Funding Approved")
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
                                    if (HttpContext.Session.GetString("UserType") == "Donor" && status == "Funding Approved")
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

            // Fetch company details for the project
            var company = await LoadCompanyByProjectId(projectId);
            if (company != null)
            {
                ViewData["CompanyName"] = company.Name ?? "Unknown Company";
                ViewData["ReferenceNumber"] = company.ReferenceNumber ?? "N/A";
                ViewData["TaxNumber"] = company.TaxNumber ?? "N/A";
                ViewData["Email"] = company.Email ?? "N/A";
                ViewData["PhoneNumber"] = company.PhoneNumber ?? "N/A";
            }
            else
            {
                _logger.LogWarning($"Company for project ID {projectId} not found.");
                ViewData["CompanyName"] = "Company information not available.";
                ViewData["ReferenceNumber"] = "N/A";
                ViewData["TaxNumber"] = "N/A";
                ViewData["Email"] = "N/A";
                ViewData["PhoneNumber"] = "N/A";
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
                                    Id = projectDoc.Id,  
                                    ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                    Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                    DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                    Status = "Funding Approved",
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

            return null; 
        }

        private async Task<Models.Company> LoadCompanyByProjectId(string projectId)
        {
            try
            {
                // Retrieve all documents from the "companies" collection
                CollectionReference companiesCollection = _firestoreDb.Collection("companies");
                QuerySnapshot companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                _logger.LogInformation("Retrieved {CompanyCount} companies from Firestore.", companiesSnapshot.Documents.Count);

                foreach (DocumentSnapshot companyDoc in companiesSnapshot.Documents)
                {
                    if (companyDoc.Exists)
                    {
                        _logger.LogInformation("Checking company with ID: {CompanyId}", companyDoc.Id);

                        // Check if the company has the project in its "projects" subcollection
                        CollectionReference projectsCollection = companyDoc.Reference.Collection("projects");
                        DocumentSnapshot projectDoc = await projectsCollection.Document(projectId).GetSnapshotAsync();

                        if (projectDoc.Exists)
                        {
                            _logger.LogInformation("Project with ID {ProjectId} found under company ID: {CompanyId}", projectId, companyDoc.Id);

                            // Convert Firestore document to the Company model
                            var company = new Models.Company
                            {
                                CompanyID = companyDoc.Id,
                                UID = companyDoc.ContainsField("UID") ? companyDoc.GetValue<string>("UID") : null,
                                Name = companyDoc.ContainsField("Name") ? companyDoc.GetValue<string>("Name") : null,
                                ReferenceNumber = companyDoc.ContainsField("ReferenceNumber") ? companyDoc.GetValue<string>("ReferenceNumber") : null,
                                TaxNumber = companyDoc.ContainsField("TaxNumber") ? companyDoc.GetValue<string>("TaxNumber") : null,
                                Description = companyDoc.ContainsField("Description") ? companyDoc.GetValue<string>("Description") : null,
                                Email = companyDoc.ContainsField("Email") ? companyDoc.GetValue<string>("Email") : null,
                                PhoneNumber = companyDoc.ContainsField("PhoneNumber") ? companyDoc.GetValue<string>("PhoneNumber") : null
                            };

                            _logger.LogInformation("Retrieved company fields: UID={UID}, Name={Name}, ReferenceNumber={ReferenceNumber}, TaxNumber={TaxNumber}, Email={Email}, PhoneNumber={PhoneNumber}",
    company.UID, company.Name, company.ReferenceNumber, company.TaxNumber, company.Email, company.PhoneNumber);

                            return company;
                        }
                        else
                        {
                            _logger.LogInformation("Project with ID {ProjectId} not found under company ID: {CompanyId}", projectId, companyDoc.Id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Company document with ID {CompanyId} does not exist.", companyDoc.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company details from Firestore.");
            }

            _logger.LogWarning("No matching company found for project ID: {ProjectId}", projectId);
            return null;
        }


    }
}
