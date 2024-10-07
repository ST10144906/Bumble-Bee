using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(FirestoreService firestoreService)
        {
            _authService = new AuthService(firestoreService);
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();  
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var userId = await _authService.LoginUserAsync(email, password);
                var userDoc = await _authService.GetUserDocumentAsync(userId);
                var userType = userDoc.GetValue<string>("Type");

                return userType switch
                {
                    "Company" => View("CompanyLoginSuccess", userId),
                    "Donor" => View("DonorLoginSuccess", userId),
                    _ => View("LoginSuccess", userId),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                ViewBag.ErrorMessage = "Login failed. Please check your email and password.";
                return View();
            }
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register (User)
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Email and password are required.";
                return View();
            }

            try
            {
                var userId = await _authService.RegisterUserAsync(email, password, "User");
                return View("RegisterSuccess", userId);
            }
            catch (ArgumentException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                ViewBag.ErrorMessage = "Registration failed. Please try again.";
                return View();
            }
        }

        // GET: Register Donor
        [HttpGet]
        public IActionResult RegisterDonor()
        {
            return View();
        }

        // POST: Register Donor
        [HttpPost]
        public async Task<IActionResult> RegisterDonor(string userEmail, string password, string confirmPassword, string donorName, string southAfricaId, string taxNumber, string phoneNumber)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                
                return View();
            }

            if (!IsValidDonorData(userEmail, donorName, southAfricaId, taxNumber, phoneNumber) ||
                !_authService.ValidatePasswordStrength(password))
            {
                ViewBag.ErrorMessage = "Please ensure all fields are correctly filled and password meets the requirements.";
                return View();
            }

            try
            {
                var userId = await _authService.RegisterDonorAsync(userEmail, password, donorName, southAfricaId, taxNumber, phoneNumber);
                return View("RegisterSuccess", userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                ViewBag.ErrorMessage = "Registration failed. Please try again.";
                return View();
            }
        }

        // GET: Register Company
        [HttpGet]
        public IActionResult RegisterCompany()
        {
            return View();
        }

        // POST: Register Company
        [HttpPost]
        public async Task<IActionResult> RegisterCompany(string userEmail, string password, string confirmPassword, string companyName, string referenceNumber, string taxNumber, string description, string phoneNumber)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (!IsValidCompanyData(userEmail, companyName, referenceNumber, taxNumber, description, phoneNumber) ||
                !_authService.ValidatePasswordStrength(password))
            {
                ViewBag.ErrorMessage = "Please ensure all fields are correctly filled and password meets the requirements.";
                return View();
            }

            try
            {
                var userId = await _authService.RegisterCompanyAsync(userEmail, password, companyName, referenceNumber, taxNumber, description, phoneNumber);
                return View("RegisterSuccess", userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                ViewBag.ErrorMessage = "Registration failed. Please try again.";
                return View();
            }
        }

        // Validation Methods
        private bool IsValidCompanyData(string email, string companyName, string referenceNumber, string taxNumber, string description, string phoneNumber)
        {
            return IsValidEmail(email) &&
                   !string.IsNullOrWhiteSpace(companyName) &&
                   IsAlphanumeric(referenceNumber) &&
                   IsAlphanumeric(taxNumber) &&
                   !string.IsNullOrWhiteSpace(description) &&
                   IsValidPhoneNumber(phoneNumber);
        }

        private bool IsValidDonorData(string email, string fullName, string idNumber, string taxNumber, string phoneNumber)
        {
            return IsValidEmail(email) &&
                   IsValidName(fullName) &&
                   IsNumeric(idNumber) &&
                   IsNumeric(taxNumber) &&
                   IsValidPhoneNumber(phoneNumber);
        }

        private bool IsValidEmail(string email)
        {
            return !string.IsNullOrEmpty(email) && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsAlphanumeric(string input)
        {
            return !string.IsNullOrEmpty(input) && Regex.IsMatch(input, @"^[a-zA-Z0-9]+$");
        }

        private bool IsNumeric(string input)
        {
            return !string.IsNullOrEmpty(input) && Regex.IsMatch(input, @"^[0-9]+$");
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            return !string.IsNullOrEmpty(phoneNumber) && Regex.IsMatch(phoneNumber, @"^\+?[1-9]\d{1,14}$");
        }

        private bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z\s]+$");
        }
    }
}
