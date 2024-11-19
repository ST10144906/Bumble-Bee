using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BumbleBeeWebApp.Controllers
{
    public class TestimonialController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<TestimonialController> _logger;

        public TestimonialController(FirestoreDb firestoreDb, ILogger<TestimonialController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        // GET: Testimonial/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Testimonial/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Testimonial testimonial)
        {
            var uid = HttpContext.Session.GetString("UserId");
            var email = HttpContext.Session.GetString("UserEmail");
            var userType = HttpContext.Session.GetString("UserType");

            // Assign session data to the testimonial model
            testimonial.UID = uid;
            testimonial.Email = email;
            testimonial.Type = userType;
            testimonial.SubmittedAt = DateTime.UtcNow;

            ModelState.Remove("UID");
            ModelState.Remove("Email");
            ModelState.Remove("Type");
            if (ModelState.IsValid)
            {
                try
                {
                    // Save the testimonial to Firestore
                    DocumentReference testimonialDocRef = _firestoreDb.Collection("testimonial").Document();
                    await testimonialDocRef.SetAsync(new
                    {
                        testimonial.UID,
                        testimonial.Email,
                        testimonial.Content,
                        testimonial.Type,
                        testimonial.SubmittedAt
                    });

                    TempData["SuccessMessage"] = "Testimonial submitted successfully!";
                    return RedirectToAction("Index", "Landing");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while submitting the testimonial.");
                    ModelState.AddModelError(string.Empty, "An error occurred while submitting the testimonial. Please try again.");
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

            return View(testimonial);
        }
    }
}
