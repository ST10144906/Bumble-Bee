using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Type;

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
                Console.WriteLine($"User Type: {userType}");

                var companyDoc = await _authService.GetCompanyDocumentByUserIdAsync(userId); 
                if (companyDoc != null)
                {
                    HttpContext.Session.SetString("UserId", userDoc.GetValue<string>("Uid"));
                    HttpContext.Session.SetString("CompanyId", companyDoc.Id);
                    HttpContext.Session.SetString("UserType", userType);
                    HttpContext.Session.SetString("UserFullName", userDoc.GetValue<string>("FullName"));
                    HttpContext.Session.SetString("UserEmail", userDoc.GetValue<string>("Email"));
                }
                else
                {
                    HttpContext.Session.SetString("UserId", userDoc.GetValue<string>("Uid"));
                    HttpContext.Session.SetString("UserType", userType);
                    HttpContext.Session.SetString("UserFullName", userDoc.GetValue<string>("FullName"));
                    HttpContext.Session.SetString("UserEmail", userDoc.GetValue<string>("Email"));
                }

                return RedirectToAction("Dashboard", "Dashboard", new { userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                ViewBag.ErrorMessage = "Login failed. Please check your email and password.";
                TempData["Message"] = "Login failed. Please check your email and password.";
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

        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Forgot Password
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage = "Please enter a valid email address.";
                return View();
            }

            try
            {
                string resetLink = await _authService.GeneratePasswordResetLinkAsync(email);
                Console.WriteLine($"Password reset link: {resetLink}");

                // Send the link manually via your email service if necessary
                // Example: await emailService.SendResetEmailAsync(email, resetLink);

                ViewBag.SuccessMessage = "Password reset link generated. Please check your email.";
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating password reset link: {ex.Message}");
                ViewBag.ErrorMessage = "Failed to generate password reset link. Please try again later.";
                return View();
            }
        }
        // POST: Register Donor
        [HttpPost]
        public async Task<IActionResult> RegisterDonor(
            string userEmail, string password, string confirmPassword,
            string donorName, string southAfricaId, string taxNumber, string phoneNumber)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (!IsValidEmail(userEmail))
            {
                ViewBag.ErrorMessage = "Invalid email format.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(donorName))
            {
                ViewBag.ErrorMessage = "Donor name is required.";
                return View();
            }

            if (!IsNumeric(southAfricaId) && !IsValidIdNumber(southAfricaId))
            {
                ViewBag.ErrorMessage = "South Africa ID must be numeric.";
                return View();
            }

            if (!IsNumeric(taxNumber) && !IsValidTaxNumber(taxNumber))
            {
                ViewBag.ErrorMessage = "Tax number must be numeric.";
                return View();
            }

            if (!IsValidPhoneNumber(phoneNumber))
            {
                ViewBag.ErrorMessage = "Phone number is invalid. It must include a country code and be in the correct format.";
                return View();
            }

            if (!_authService.ValidatePasswordStrength(password))
            {
                ViewBag.ErrorMessage = "Password does not meet the strength requirements.";
                return View();
            }

            try
            {
                var userId = await _authService.RegisterDonorAsync(userEmail, password, donorName, southAfricaId, taxNumber, phoneNumber);
                return View("RegisterSuccess");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                ViewBag.ErrorMessage = "Registration failed. Please try again.";
                return View();
            }
        }

        // GET: Register Admin
        [HttpGet]
        public IActionResult RegisterAdministrator()
        {
            return View();
        }

        // POST: Register Admin
        [HttpPost]
        public async Task<IActionResult> RegisterAdministrator(string userEmail, string password, string confirmPassword, string fullName, string userRole) 
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (!IsValidUserRole(userRole))
            {
                ViewBag.ErrorMessage = "No user role is selected.";
                return View();
            }

            if (!IsValidEmail(userEmail))
            {
                ViewBag.ErrorMessage = "Invalid email format.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ViewBag.ErrorMessage = "Donor name is required.";
                return View();
            }

            if (!_authService.ValidatePasswordStrength(password))
            {
                ViewBag.ErrorMessage = "Password does not meet the strength requirements.";
                return View();
            }

            try
            {
                var userId = await _authService.RegisterAdministratorAsync(userEmail, password, fullName, userRole);
                return View("RegisterSuccess");
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
        public async Task<IActionResult> RegisterCompany(
            string userEmail, string password, string confirmPassword,
        string fullName, string description, string phoneNumber)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (!IsValidEmail(userEmail))
            {
                ViewBag.ErrorMessage = "Invalid email format.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ViewBag.ErrorMessage = "Company name is required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.ErrorMessage = "Description is required.";
                return View();
            }

            if (!IsValidPhoneNumber(phoneNumber))
            {
                ViewBag.ErrorMessage = "Phone number is invalid. It must include a country code and be in the correct format.";
                return View();
            }

            if (!_authService.ValidatePasswordStrength(password))
            {
                ViewBag.ErrorMessage = "Password does not meet the strength requirements.";
                return View();
            }

            try
            {
                var userId = await _authService.RegisterCompanyAsync(userEmail, password, fullName, description, phoneNumber);
                Console.WriteLine($"Company registered successfully. UserId: {userId}");
                return View("RegisterSuccess");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                ViewBag.ErrorMessage = $"Registration failed: {ex.Message}";
                return View();
            }
        }


        // Validation Methods
        private bool IsValidCompanyData(string email, string companyName, string description, string phoneNumber)
        {
            return IsValidEmail(email) &&
                   !string.IsNullOrWhiteSpace(companyName) &&
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

        private bool IsValidUserRole(string userRole)
        {
            return !string.IsNullOrEmpty(userRole);
        }

        private bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z\s]+$");
        }

        private bool IsValidIdNumber(string idNumber)
        {
            // Example: South African ID number validation (13 digits, numeric)
            return !string.IsNullOrEmpty(idNumber) && Regex.IsMatch(idNumber, @"^\d{13}$");
        }

        private bool IsValidTaxNumber(string taxNumber)
        {
            // Example: South African Tax Number validation (10 digits, numeric)
            return !string.IsNullOrEmpty(taxNumber) && Regex.IsMatch(taxNumber, @"^\d{10}$");
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string email)
        {
            try
            {
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);

                if (userRecord != null)
                {
                    if (!userRecord.EmailVerified)
                    {
                        return View("RegisterSuccess"); 
                    }

                    return View("EmailAlreadyVerified"); 
                }

                return View("Error"); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email verification failed: {ex.Message}");
                return View("Error"); 
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Landing");
        }



    }
}
