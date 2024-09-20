using BumbleBeeWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BumbleBeeWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FirestoreService _firestoreService;
        private readonly AuthService _authService;
        private readonly StorageService _storageService;
        private readonly FunctionsService _functionsService;

        public HomeController(
            ILogger<HomeController> logger,
            FirestoreService firestoreService,
            AuthService authService,
            StorageService storageService,
            FunctionsService functionsService)
        {
            _logger = logger;
            _firestoreService = firestoreService;
            _authService = authService;
            _storageService = storageService;
            _functionsService = functionsService;
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index(string action)
        {
            if (HttpContext.Request.Method == "POST")
            {
                switch (action)
                {
                    case "Authentication":
                        try
                        {
                            string uid = await _authService.CreateUserAsync("john@example.com", "password123");
                            ViewData["Message"] = $"User created with UID: {uid}";
                        }
                        catch (Exception ex)
                        {
                            ViewData["Message"] = $"User creation failed: {ex.Message}";
                            _logger.LogError(ex, "User creation failed.");
                        }
                        break;

                    case "Firestore":
                        try
                        {
                            await _firestoreService.AddDocumentAsync("users", new { Name = "John Doe", Email = "john@example.com" });
                            ViewData["Message"] = "Document added to Firestore.";
                        }
                        catch (Exception ex)
                        {
                            ViewData["Message"] = $"Document addition to Firestore failed: {ex.Message}";
                            _logger.LogError(ex, "Document addition to Firestore failed.");
                        }
                        break;

                    case "Storage":
                        try
                        {
                            string basePath = AppContext.BaseDirectory;
                            string projectRelativePath = Path.Combine(basePath, "..", "..", "..", "storagetest.txt");

                            if (!System.IO.File.Exists(projectRelativePath))
                            {
                                ViewData["Message"] = "File not found.";
                                _logger.LogError("File not found at path: {FilePath}", projectRelativePath);
                                break;
                            }

                            await _storageService.UploadFileAsync(projectRelativePath, "uploaded-files/file.txt");
                            ViewData["Message"] = "File uploaded to Firebase Storage.";
                        }
                        catch (Exception ex)
                        {

                            ViewData["Message"] = $"File upload failed: {ex.Message}";
                            _logger.LogError(ex, "File upload to Firebase Storage failed.");
                        }
                        break;


                    case "Function":
                        try
                        {
                            // string functionResponse = await _functionsService.CallFunctionAsync("yourFunctionName", new { key = "value" });
                            ViewData["Message"] = $"Firebase Function response: (functionResponse)";
                        }
                        catch (Exception ex)
                        {

                        }
                        break;

                    default:
                        ViewData["Message"] = "Action Failed.";
                        break;
                }
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}