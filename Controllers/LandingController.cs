using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class LandingController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public LandingController(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<IActionResult> Index()
        {
            List<Testimonial> testimonials = new List<Testimonial>();

            try
            {
                QuerySnapshot snapshot = await _firestoreDb.Collection("testimonial").GetSnapshotAsync();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var testimonial = document.ConvertTo<Testimonial>();
                        testimonials.Add(testimonial);
                    }
                }
            }
            catch
            {
                ViewBag.TestimonialError = "Unable to load testimonials at this time.";
            }

            return View(testimonials);
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
