using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class LandingController : Controller
    {
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _firestoreDb;

        public LandingController(FirestoreDb firestoreDb, FirestoreService firestoreService)
        {
            _firestoreDb = firestoreDb;
            _firestoreService = firestoreService;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch a random testimonial
            var randomTestimonial = await _firestoreService.GetRandomTestimonialAsync();

            // Pass the testimonial to the view
            return View(randomTestimonial);
        }


        public IActionResult AboutUs()
        {
            return View();
        }

        // Navbar Methods
        public IActionResult LoginPage()
        {
            return RedirectToAction("Login", "Account");
        }

        public IActionResult RegisterPage()
        {
            return RedirectToAction("Register", "Account");
        }

        // Button Actions
        public IActionResult LearnMore()
        {
            // Redirects to the mission section on the landing page
            return RedirectToAction("Index", "Landing", new { section = "mission" });
        }

        public IActionResult TestimonailCreatePage()
        {
            if (HttpContext.Session.GetString("UserType") == null)
            {
                TempData["Message"] = "You need to log in to submit a testimonial.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                return RedirectToAction("Create", "Testimonial");
            }
        }

        // --- Payment Page
        public IActionResult DonateNow()
        {
            return RedirectToAction("Index", "Payment");
        }
    }
}
