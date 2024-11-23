using BumbleBeeWebApp.Controllers.Company;
using BumbleBeeWebApp.Models;
using Firebase.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class UserController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<CompanyController> _logger;

        public UserController(FirestoreDb firestoreDb, ILogger<CompanyController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserType") == "Company")
            {
                var company = await LoadCompanyUser();
                return View("~/Views/User/ViewCompanyProfile.cshtml", company);
            }
            else
            {
                var donor = await LoadDonorUser();
                return View("~/Views/User/ViewDonorProfile.cshtml", donor);
            }
            
        }
        public async Task<Donor> LoadDonorUser()
        {
            var uid = HttpContext.Session.GetString("UserId");
            Console.WriteLine(uid);

            try
            {
                // Retrieve all documents from the "users" collection
                CollectionReference donorsCollection = _firestoreDb.Collection("users");
                QuerySnapshot donorsSnapshot = await donorsCollection.GetSnapshotAsync();

                foreach (DocumentSnapshot donorDoc in donorsSnapshot.Documents)
                {
                    if (donorDoc.Exists && donorDoc.ContainsField("Uid") && donorDoc.GetValue<string>("Uid") == uid)
                    {
                        // Map the Firestore document to the Donor model
                        return new Donor
                        {
                            DonorName = donorDoc.ContainsField("FullName") ? donorDoc.GetValue<string>("FullName") : "Unknown Donor",
                            SouthAfricaId = donorDoc.ContainsField("IdNumber") ? donorDoc.GetValue<string>("IdNumber") : null,
                            TaxNumber = donorDoc.ContainsField("TaxNumber") ? donorDoc.GetValue<string>("TaxNumber") : null,
                            Email = donorDoc.ContainsField("Email") ? donorDoc.GetValue<string>("Email") : null,
                            PhoneNumber = donorDoc.ContainsField("PhoneNumber") ? donorDoc.GetValue<string>("PhoneNumber") : null
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donor details from Firestore.");
            }

            return null; // Return null if no matching donor is found
        }

        public async Task<UserAndCompanyViewModel> LoadCompanyUser()
        {
            var uid = HttpContext.Session.GetString("UserId");
            Console.WriteLine(uid);

            try
            {
                // Initialize the ViewModel
                var viewModel = new UserAndCompanyViewModel();

                // Retrieve all documents from the "users" collection
                CollectionReference usersCollection = _firestoreDb.Collection("users");
                QuerySnapshot usersSnapshot = await usersCollection.GetSnapshotAsync();

                // Loop through user documents to find the matching UID
                foreach (DocumentSnapshot userDoc in usersSnapshot.Documents)
                {
                    if (userDoc.Exists && userDoc.ContainsField("Uid") && userDoc.GetValue<string>("Uid") == uid)
                    {
                        viewModel.UserEmail = userDoc.ContainsField("Email") ? userDoc.GetValue<string>("Email") : "Unknown Email";
                        viewModel.UserFullName = userDoc.ContainsField("FullName") ? userDoc.GetValue<string>("FullName") : "Unknown Name";
                        viewModel.UserJobRole = userDoc.ContainsField("JobRole") ? userDoc.GetValue<string>("JobRole") : "Unknown Role";
                        viewModel.UserPhoneNumber = userDoc.ContainsField("PhoneNumber") ? userDoc.GetValue<string>("PhoneNumber") : "Unknown Phone";
                        viewModel.UserType = userDoc.ContainsField("Type") ? userDoc.GetValue<string>("Type") : "Unknown Type";
                        break; // Stop looping once the matching user is found
                    }
                }

                // Retrieve all documents from the "companies" collection
                CollectionReference companiesCollection = _firestoreDb.Collection("companies");
                QuerySnapshot companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                // Loop through company documents to find the matching UID
                foreach (DocumentSnapshot companyDoc in companiesSnapshot.Documents)
                {
                    if (companyDoc.Exists && companyDoc.ContainsField("UID") && companyDoc.GetValue<string>("UID") == uid)
                    {
                        viewModel.CompanyName = companyDoc.ContainsField("Name") ? companyDoc.GetValue<string>("Name") : "Unknown Company";
                        viewModel.CompanyReferenceNumber = companyDoc.ContainsField("ReferenceNumber") ? companyDoc.GetValue<string>("ReferenceNumber") : "Unknown Role";
                        viewModel.CompanyTaxNumber = companyDoc.ContainsField("TaxNumber") ? companyDoc.GetValue<string>("TaxNumber") : "Unknown Phone";
                        viewModel.CompanyEmail = companyDoc.ContainsField("Email") ? companyDoc.GetValue<string>("Email") : "Unknown Email";
                        break; // Stop looping once the matching company is found
                    }
                }

                return viewModel; // Return the populated ViewModel
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user and company details from Firestore.");
            }

            return null; // Return null if no matching data is found
        }
    }
}
