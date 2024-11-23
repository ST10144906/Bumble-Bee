using BumbleBeeWebApp.Models;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace BumbleBeeWebApp.Controllers.Admin
{
    public class AdminManagerController : Controller
    {
        private readonly AuthService _authService;
        private readonly FirestoreService _firestoreService;
        private readonly ILogger<DashboardController> _logger;

        public AdminManagerController(FirestoreService firestoreService, ILogger<DashboardController> logger)
        {
            _authService = new AuthService(firestoreService);
            _firestoreService = firestoreService;
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
                        PhoneNumber = companyData.ContainsKey("PhoneNumber") ? companyData["PhoneNumber"]?.ToString() : string.Empty
                    };

                    companies.Add(company);
                }
            }

            return View("~/Views/Admin/CompanyManager.cshtml", companies);
        }


    }
}
