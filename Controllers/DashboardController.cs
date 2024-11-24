﻿using BumbleBeeWebApp.Models;
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
            _logger.LogInformation("Dashboard requested for UserId: {UserId}", userId);

            var userType = HttpContext.Session.GetString("UserType");
            var viewModel = new DashboardViewModel { UserType = userType };

            if (userType == "Company")
            {
                string companyId = HttpContext.Session.GetString("CompanyId");
                if (companyId == null)
                {
                    var companyDocument = await _authService.GetCompanyDocumentByUserIdAsync(userId);
                    if (companyDocument != null)
                    {
                        companyId = companyDocument.Id;
                        HttpContext.Session.SetString("CompanyId", companyId);
                        _logger.LogInformation("User {UserId} is part of company with CompanyId: {CompanyId}", userId, companyId);
                    }
                }

                if (companyId != null)
                {
                    _logger.LogInformation("Loading projects for Company with ID: {CompanyId}", companyId);
                    viewModel.FundingApprovedProjects = await LoadProjectsByStatus(companyId, "Funding Approved");
                    viewModel.PendingApprovalProjects = await LoadProjectsByStatus(companyId, "Pending Approval");
                }
            }
            else
            {
                _logger.LogInformation("Loading all projects for Admin/Donor.");
                viewModel.Projects = await LoadAllProjects();
            }

            return View(viewModel);
        }


        private async Task<List<Project>> LoadAllProjects()
        {
            List<Project> allProjects = new List<Project>();
            _logger.LogInformation("Loading all projects from Firestore...");

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
                        _logger.LogInformation("Processing company with ID: {CompanyId}", companyDoc.Id);

                        // Get the "projects" subcollection for each company
                        CollectionReference projectsCollection = companyDoc.Reference.Collection("projects");
                        QuerySnapshot projectsSnapshot = await projectsCollection.GetSnapshotAsync();
                        _logger.LogInformation("Retrieved {ProjectCount} projects for CompanyId: {CompanyId}", projectsSnapshot.Documents.Count, companyDoc.Id);

                        foreach (DocumentSnapshot projectDoc in projectsSnapshot.Documents)
                        {
                            if (projectDoc.Exists)
                            {
                                try
                                {
                                    string status = projectDoc.ContainsField("Status") ? projectDoc.GetValue<string>("Status") : null;

                                    if (HttpContext.Session.GetString("UserType") == "Admin" && status == "Pending Approval")
                                    {
                                        var project = new Project
                                        {
                                            Id = projectDoc.Id,
                                            ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                            Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                            DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                            MiscellaneousDocumentsUrl = projectDoc.ContainsField("MiscellaneousDocumentsUrl") ? projectDoc.GetValue<string>("MiscellaneousDocumentsUrl") : null
                                        };

                                        allProjects.Add(project);
                                        _logger.LogInformation("Added project {ProjectId} with status {Status}", project.Id, status);
                                    }

                                    if (HttpContext.Session.GetString("UserType") == "Donor" && status == "Funding Approved")
                                    {
                                        var project = new Project
                                        {
                                            Id = projectDoc.Id,
                                            ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                            Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                            DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                            MiscellaneousDocumentsUrl = projectDoc.ContainsField("MiscellaneousDocumentsUrl") ? projectDoc.GetValue<string>("MiscellaneousDocumentsUrl") : null
                                        };

                                        allProjects.Add(project);
                                        _logger.LogInformation("Added project {ProjectId} with status {Status}", project.Id, status);
                                    }
                                }
                                catch (Exception innerEx)
                                {
                                    _logger.LogError(innerEx, "Error processing project document ID: {ProjectId}", projectDoc.Id);
                                }
                            }
                        }
                    }
                }

                // Sort projects by DateCreated (most recent first)
                allProjects = allProjects.OrderByDescending(p => p.DateCreated).ToList();
                _logger.LogInformation("Sorted {ProjectCount} projects by DateCreated", allProjects.Count);

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
                _logger.LogWarning("ProjectDetails: ProjectId is null or empty.");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for project with ID: {ProjectId}", projectId);

            var project = await LoadProjectById(projectId);
            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found.", projectId);
                return NotFound();
            }

            // Fetch the requested funding amount
            decimal requestedAmount = await GetRequestedFundingAmount(projectId);

            // Fetch the received donations amount
            decimal receivedAmount = await GetReceivedFundingAmount(project.ProjectName);

            // Pass the funding amounts to the view
            ViewData["RequestedAmount"] = requestedAmount;
            ViewData["ReceivedAmount"] = receivedAmount;

            // Fetch company details for the project
            var company = await LoadCompanyByProjectId(projectId);
            if (company != null)
            {
                ViewData["CompanyId"] = company.CompanyID ?? "Unknown CompanyId";
                ViewData["CompanyName"] = company.Name ?? "Unknown Company";
                ViewData["ReferenceNumber"] = company.ReferenceNumber ?? "N/A";
                ViewData["TaxNumber"] = company.TaxNumber ?? "N/A";
                ViewData["Email"] = company.Email ?? "N/A";
                ViewData["PhoneNumber"] = company.PhoneNumber ?? "N/A";
                _logger.LogInformation("Company details for project ID {ProjectId} retrieved successfully.", projectId);
            }
            else
            {
                _logger.LogWarning("Company for project ID {ProjectId} not found.", projectId);
                ViewData["CompanyName"] = "Company information not available.";
                ViewData["ReferenceNumber"] = "N/A";
                ViewData["TaxNumber"] = "N/A";
                ViewData["Email"] = "N/A";
                ViewData["PhoneNumber"] = "N/A";
            }

            return View(project);
        }

        // Method to get the requested funding amount for a project
        private async Task<decimal> GetRequestedFundingAmount(string projectId)
        {
            decimal requestedAmount = 0;

            try
            {
                var fundingRequestsCollection = _firestoreDb.Collection("companies")
                    .Document("companyId") 
                    .Collection("fundingRequests");

                var query = fundingRequestsCollection.WhereEqualTo("projectId", projectId);
                var snapshot = await query.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        requestedAmount += document.GetValue<decimal>("Amount");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching requested funding for project with ID: {ProjectId}", projectId);
            }

            return requestedAmount;
        }

        // Method to get the received funding amount for a project
        private async Task<decimal> GetReceivedFundingAmount(string projectName)
        {
            decimal receivedAmount = 0;

            try
            {
                var donationsCollection = _firestoreDb.Collection("donations");
                var query = donationsCollection.WhereEqualTo("projectname", projectName);
                var snapshot = await query.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        receivedAmount += document.GetValue<decimal>("Amount");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching received donations for project with name: {ProjectName}", projectName);
            }

            return receivedAmount;
        }


        private async Task<Project> LoadProjectById(string projectId)
        {
            try
            {
                _logger.LogInformation("Loading project by ID: {ProjectId}", projectId);

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
                            if (projectDoc.Exists && projectDoc.Id == projectId)
                            {
                                _logger.LogInformation("Project with ID {ProjectId} found.", projectId);
                                return new Project
                                {
                                    Id = projectDoc.Id,
                                    ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                    Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                    DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                    Status = projectDoc.ContainsField("Status") ? projectDoc.GetValue<string>("Status") : "No Description",
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
                _logger.LogInformation("Loading company by project ID: {ProjectId}", projectId);

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

        private async Task<List<Project>> LoadProjectsByStatus(string companyId, string status)
        {
            List<Project> projects = new List<Project>();

            try
            {
                _logger.LogInformation("Starting to load projects with status: {Status} for Company ID: {CompanyId}", status, companyId);

                CollectionReference companiesCollection = _firestoreDb.Collection("companies");
                QuerySnapshot companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                foreach (DocumentSnapshot companyDoc in companiesSnapshot.Documents)
                {
                    if (companyId != null && companyDoc.Id != companyId)
                        continue;

                    _logger.LogDebug("Processing company document with ID: {CompanyDocId}", companyDoc.Id);

                    var projectsCollection = companyDoc.Reference.Collection("projects");
                    var projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                    foreach (var projectDoc in projectsSnapshot.Documents)
                    {
                        if (projectDoc.Exists && projectDoc.ContainsField("Status") &&
                            projectDoc.GetValue<string>("Status") == status)
                        {
                            var project = new Project
                            {
                                Id = projectDoc.Id,
                                ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                MiscellaneousDocumentsUrl = projectDoc.ContainsField("MiscellaneousDocumentsUrl") ? projectDoc.GetValue<string>("MiscellaneousDocumentsUrl") : null
                            };

                            projects.Add(project);

                            _logger.LogDebug("Added project: {ProjectName} (ID: {ProjectId}, Status: {Status}, Created: {DateCreated})",
                                project.ProjectName, project.Id, status, project.DateCreated);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects with status {Status} from Firestore.", status);
            }

            _logger.LogInformation("Completed loading projects with status: {Status}. Total projects found: {Count}", status, projects.Count);
            return projects.OrderByDescending(p => p.DateCreated).ToList();
        }


        public async Task<IActionResult> DonateToProject(string selectedProjectName) 
        {
            Console.WriteLine($"Selected Project Name: {selectedProjectName}");
            if (HttpContext.Session.GetString("UserType") == null)
            {
                TempData["Message"] = "You need to log in to create a donation.";
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("UserType") != "Donor")
            {
                TempData["Message"] = "You need to login as a Donor to create a donation.";
                return RedirectToAction("Login", "Account");
            }

            // If a project name is already provided, skip the lookup
            if (string.IsNullOrEmpty(selectedProjectName))
            {
                try
                {
                    // Query Firestore to find the first project with "Funding Approved" status
                    var companiesCollection = _firestoreDb.Collection("companies");
                    var companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                    foreach (var companyDoc in companiesSnapshot.Documents)
                    {
                        var projectsCollection = companiesCollection.Document(companyDoc.Id).Collection("projects");
                        var projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                        foreach (var projectDoc in projectsSnapshot.Documents)
                        {
                            if (projectDoc.TryGetValue("ProjectName", out string projectName) &&
                                projectDoc.TryGetValue("Status", out string status) &&
                                status == "Funding Approved")
                            {
                                selectedProjectName = projectName;
                                break; // Exit loop once a valid project is found
                            }
                        }

                        if (!string.IsNullOrEmpty(selectedProjectName))
                            break; // Stop iterating through companies if a project is found
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching projects: {ex.Message}");
                    TempData["Message"] = "Error fetching projects. Please try again later.";
                    return RedirectToAction("Index", "Landing");
                }
            }

            if (string.IsNullOrEmpty(selectedProjectName))
            {
                TempData["Message"] = "No available projects with 'Funding Approved' status.";
                return RedirectToAction("Index", "Landing");
            }

            // Redirect to the Payment page with the selected project name
            return RedirectToAction("Index", "Payment", new { selectedProject = selectedProjectName });
        }

        // Method to Approve FUnding of the Projects
        [HttpPost]
        public async Task<IActionResult> ApproveFunding(string companyId, string projectId)
        {
            try
            {
                _logger.LogInformation("Entering ApproveFunding action for Company ID: {CompanyId}, Project ID: {ProjectId}", companyId, projectId);

                // Reference to the project document
                DocumentReference projectDocRef = _firestoreDb
                    .Collection("companies")
                    .Document(companyId)
                    .Collection("projects")
                    .Document(projectId);

                // Fetch the project data
                DocumentSnapshot projectSnapshot = await projectDocRef.GetSnapshotAsync();

                if (projectSnapshot.Exists)
                {
                    var projectData = projectSnapshot.ToDictionary();
                    if (projectData.TryGetValue("Status", out var currentStatus) && currentStatus.ToString() == "Pending Approval")
                    {
                        // Update the project status
                        var updates = new Dictionary<string, object>
                        {
                            { "Status", "Approved Funding" }
                        };

                        await projectDocRef.UpdateAsync(updates);
                        _logger.LogInformation("Project status updated to Approved Funding for Project ID: {ProjectId}", projectId);
                        TempData["Success"] = "Project funding approved successfully.";
                    }
                    else
                    {
                        _logger.LogWarning("Project status is not Pending Approval for Project ID: {ProjectId}", projectId);
                        TempData["Error"] = "Project is not in Pending Approval status.";
                    }
                }
                else
                {
                    _logger.LogError("Project not found with ID: {ProjectId}", projectId);
                    TempData["Error"] = "Project not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving project funding for Company ID: {CompanyId}, Project ID: {ProjectId}", companyId, projectId);
                TempData["Error"] = "An error occurred while approving the project funding.";
            }

            return RedirectToAction("Dashboard", "Dashboard");
        }

    }
}

