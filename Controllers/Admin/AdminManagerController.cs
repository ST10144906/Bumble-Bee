using BumbleBeeWebApp.Models;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace BumbleBeeWebApp.Controllers.Admin
{
    public class AdminManagerController : Controller
    {
        private readonly AuthService _authService;
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _firestoreDb;
        private readonly StorageService _storageService;
        private readonly ILogger<DashboardController> _logger;

        public AdminManagerController(FirestoreService firestoreService, StorageService storageService, FirestoreDb firestoreDb, ILogger<DashboardController> logger)
        {
            _authService = new AuthService(firestoreService);
            _firestoreService = firestoreService;
            _firestoreDb = firestoreDb;
            _storageService = storageService;
            _logger = logger;
        }

        // GET : Gets all Admin users and loads the page
        [HttpGet]
        public async Task<IActionResult> LoadAdminUsers()
        {
            List<User> users = new List<User>();

            // Get the collection of users from Firestore
            QuerySnapshot userQuerySnapshot = await _firestoreService.GetCollectionAsync("users");

            foreach (DocumentSnapshot document in userQuerySnapshot.Documents)
            {
                if (document.Exists)
                {
                    Dictionary<string, object> userData = document.ToDictionary();
                    User user = new User
                    {
                        UID = document.Id,
                        FullName = userData.ContainsKey("FullName") ? userData["FullName"]?.ToString() : string.Empty,
                        Email = userData.ContainsKey("Email") ? userData["Email"]?.ToString() : string.Empty,
                        Role = userData.ContainsKey("Type") ? userData["Type"]?.ToString() : string.Empty
                    };

                    // Filter users with the role "Admin"
                    if (user.Role == "Admin" || user.Role == "Auditor")
                    {
                        users.Add(user);
                    }
                }
            }

            return View("~/Views/Admin/AdminManager.cshtml", users);
        }

        public async Task<IActionResult> Delete(string documentUid, string deleteEmail)
        {
            try
            {
                // Fetch user data from Firestore to get the email
                DocumentSnapshot documentSnapshot = await _firestoreService.GetDocumentAsync("users", documentUid);
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(deleteEmail);

                var authId = userRecord.Uid;
                Console.WriteLine(documentUid);
                Console.WriteLine(authId);

                if (documentSnapshot.Exists)
                {
                    // Get email from the Firestore document
                    Dictionary<string, object> userData = documentSnapshot.ToDictionary();
                    string email = userData.ContainsKey("Email") ? userData["Email"]?.ToString() : null;

                    if (string.IsNullOrEmpty(email))
                    {
                        TempData["ErrorMessage"] = "User email not found.";
                        return await LoadAdminUsers();
                    }

                    // Step 1: Delete the user from Firestore
                    await _firestoreService.DeleteUserFromFirestoreAsync(documentUid);

                    // Step 2: Delete the user from Firebase Authentication
                    await FirebaseAuth.DefaultInstance.DeleteUserAsync(authId);

                    TempData["SuccessMessage"] = "User successfully deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "User not found in Firestore.";
                }
            }
            catch (FirebaseAuthException ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user from FirebaseAuth: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }

            return await LoadAdminUsers(); // Redirect to your user listing page
        }

        public async Task<IActionResult> LoadDonorUsers()
        {
            List<Donor> donors = new List<Donor>();

            // Get the collection of users from Firestore
            QuerySnapshot userQuerySnapshot = await _firestoreService.GetCollectionAsync("users");

            foreach (DocumentSnapshot document in userQuerySnapshot.Documents)
            {
                if (document.Exists)
                {
                    Dictionary<string, object> userData = document.ToDictionary();

                    // Map Firestore data to the Donor model
                    Donor donor = new Donor
                    {
                        UID = document.Id,
                        DonorName = userData.ContainsKey("FullName") ? userData["FullName"]?.ToString() : string.Empty,
                        Email = userData.ContainsKey("Email") ? userData["Email"]?.ToString() : string.Empty,
                        SouthAfricaId = userData.ContainsKey("IdNumber") ? userData["IdNumber"]?.ToString() : string.Empty,
                        TaxNumber = userData.ContainsKey("TaxNumber") ? userData["TaxNumber"]?.ToString() : string.Empty,
                        PhoneNumber = userData.ContainsKey("PhoneNumber") ? userData["PhoneNumber"]?.ToString() : string.Empty
                    };

                    // Filter users with the role "Donor"
                    if (userData.ContainsKey("Type") && userData["Type"]?.ToString() == "Donor")
                    {
                        donors.Add(donor);
                    }
                }
            }

            return View("~/Views/Admin/DonorManager.cshtml", donors);
        }

        public async Task<IActionResult> DeleteDonor(string documentUid, string deleteEmail)
        {
            try
            {
                // Fetch user data from Firestore to get the email
                DocumentSnapshot documentSnapshot = await _firestoreService.GetDocumentAsync("users", documentUid);
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(deleteEmail);

                var authId = userRecord.Uid;
                Console.WriteLine(documentUid);
                Console.WriteLine(authId);

                if (documentSnapshot.Exists)
                {
                    // Get email from the Firestore document
                    Dictionary<string, object> userData = documentSnapshot.ToDictionary();
                    string email = userData.ContainsKey("Email") ? userData["Email"]?.ToString() : null;

                    if (string.IsNullOrEmpty(email))
                    {
                        TempData["ErrorMessage"] = "User email not found.";
                        return await LoadDonorUsers();
                    }

                    // Step 1: Delete the user from Firestore
                    await _firestoreService.DeleteUserFromFirestoreAsync(documentUid);

                    // Step 2: Delete the user from Firebase Authentication
                    await FirebaseAuth.DefaultInstance.DeleteUserAsync(authId);

                    TempData["SuccessMessage"] = "User successfully deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "User not found in Firestore.";
                }
            }
            catch (FirebaseAuthException ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user from FirebaseAuth: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }

            return await LoadDonorUsers();
        }

        public async Task<IActionResult> LoadTestimonials()
        {
            List<Testimonial> testimonials = new List<Testimonial>();

            // Get the collection of testimonials from Firestore
            QuerySnapshot testimonialQuerySnapshot = await _firestoreService.GetCollectionAsync("testimonial");

            foreach (DocumentSnapshot document in testimonialQuerySnapshot.Documents)
            {
                if (document.Exists)
                {
                    Dictionary<string, object> testimonialData = document.ToDictionary();

                    // Map Firestore data to the Testimonial model
                    Testimonial testimonial = new Testimonial
                    {
                        UID = document.Id, // this sets it to the document uid not the uid of the use that made it 
                        Email = testimonialData.ContainsKey("Email") ? testimonialData["Email"]?.ToString() : string.Empty,
                        Content = testimonialData.ContainsKey("Content") ? testimonialData["Content"]?.ToString() : string.Empty,
                        Type = testimonialData.ContainsKey("Type") ? testimonialData["Type"]?.ToString() : string.Empty,
                        SubmittedAt = testimonialData.ContainsKey("SubmittedAt") && DateTime.TryParse(testimonialData["SubmittedAt"]?.ToString(), out DateTime date)
                            ? date
                            : DateTime.UtcNow
                    };

                    testimonials.Add(testimonial);
                }
            }

            return View("~/Views/Admin/TestimonialManager.cshtml", testimonials);
        }

        public async Task<IActionResult> DeleteTestimonial(string documentUid)
        {
            if (string.IsNullOrWhiteSpace(documentUid))
            {
                TempData["ErrorMessage"] = "Invalid testimonial ID.";
                return RedirectToAction("LoadTestimonials");
            }

            try
            {
                // Attempt to delete the document from Firestore
                await _firestoreService.DeleteDocumentAsync("testimonial", documentUid);

                TempData["SuccessMessage"] = "Testimonial deleted successfully.";
            }
            catch (Exception ex)
            {

            }
            return await LoadTestimonials();
        }

        public async Task<IActionResult> LoadCompanies()
        {
            List<Models.Company> companies = new List<Models.Company>();

            // Get the collection of companies from Firestore
            QuerySnapshot companiesQuerySnapshot = await _firestoreService.GetCollectionAsync("companies");

            foreach (DocumentSnapshot document in companiesQuerySnapshot.Documents)
            {
                if (document.Exists)
                {
                    Dictionary<string, object> companyData = document.ToDictionary();

                    // Map Firestore data to the Company model
                    Models.Company company = new Models.Company
                    {
                        CompanyID = document.Id, // Setting the Firestore document ID as CompanyID
                        UID = companyData.ContainsKey("UID") ? companyData["UID"]?.ToString() : string.Empty,
                        Name = companyData.ContainsKey("Name") ? companyData["Name"]?.ToString() : string.Empty,
                        ReferenceNumber = companyData.ContainsKey("ReferenceNumber") ? companyData["ReferenceNumber"]?.ToString() : string.Empty,
                        TaxNumber = companyData.ContainsKey("TaxNumber") ? companyData["TaxNumber"]?.ToString() : string.Empty,
                        Email = companyData.ContainsKey("Email") ? companyData["Email"]?.ToString() : string.Empty,
                        PhoneNumber = companyData.ContainsKey("PhoneNumber") ? companyData["PhoneNumber"]?.ToString() : string.Empty,
                        ApprovalStatus = companyData.ContainsKey("ApprovalStatus") ? companyData["ApprovalStatus"]?.ToString() : string.Empty,
                        DocumentUrl = companyData.ContainsKey("DocumentUrl") ? companyData["DocumentUrl"]?.ToString() : string.Empty
                    };

                    companies.Add(company);
                }
            }

            return View("~/Views/Admin/CompanyManager.cshtml", companies);
        }

        public async Task<IActionResult> UpdateApproval(string companyId)
        {
            Console.WriteLine("Company Id: " + companyId);

            try
            {
                // Reference the company document in Firestore
                DocumentReference companyDocRef = _firestoreDb.Collection("companies").Document(companyId);

                // Get the company snapshot
                DocumentSnapshot companySnapshot = await companyDocRef.GetSnapshotAsync();

                if (!companySnapshot.Exists)
                {
                    _logger.LogWarning("Company document does not exist for Company ID: {CompanyId}", companyId);
                    TempData["Error"] = "Company does not exist.";
                    return await LoadCompanies();
                }

                // Convert the snapshot to a dictionary
                var companyData = companySnapshot.ToDictionary();

                // Check if the ApprovalStatus field exists
                if (!companyData.TryGetValue("ApprovalStatus", out var currentStatus))
                {
                    _logger.LogWarning("ApprovalStatus field is missing for Company ID: {CompanyId}", companyId);
                    TempData["Error"] = "Approval status is missing for the company.";
                    return await LoadCompanies();
                }

                // Update the ApprovalStatus based on its current value
                string updatedStatus;

                if (currentStatus.ToString() == "Pending Approval")
                {
                    updatedStatus = "Approved";
                    TempData["Success"] = "Company funding approved successfully.";
                    _logger.LogInformation("Company status updated to Approved for Company ID: {CompanyId}", companyId);
                }
                else
                {
                    updatedStatus = "Pending Approval";
                    TempData["Success"] = "Company status set to Pending Approval.";
                    _logger.LogInformation("Company status updated to Pending Approval for Company ID: {CompanyId}", companyId);
                }

                // Perform the update in Firestore
                var updates = new Dictionary<string, object>
        {
            { "ApprovalStatus", updatedStatus } 
        };
                await companyDocRef.UpdateAsync(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating approval for Company ID: {CompanyId}", companyId);
                TempData["Error"] = "An error occurred while updating the company approval.";
            }

            return await LoadCompanies(); // Placeholder for redirection or reloading
        }

        public async Task<IActionResult> DownloadDocument(string documentUrl)
        {
            Console.WriteLine(documentUrl);
            try
            {
                // Extract the object name from the URL
                var uri = new Uri(documentUrl);
                var objectName = uri.AbsolutePath.TrimStart('/');
                Console.WriteLine($"Object Name: {objectName}");



                // Download the file
                var fileBytes = await _storageService.DownloadFileAsync(objectName);

                // Get the file name from the URL
                var fileName = Path.GetFileName(uri.LocalPath);

                // Return the file as a download
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                // Handle errors
                Console.WriteLine($"Error downloading document: {ex.Message}");
                return BadRequest("Error downloading the document.");
            }
        }
    }
}
