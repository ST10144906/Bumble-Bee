using Microsoft.AspNetCore.Mvc;

namespace BumbleBeeWebApp.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult UserDash()
        {
            return RedirectToAction("UserDashboard","Dashboard");
        }
        public IActionResult AdminDash()
        {
            return RedirectToAction("AdminDashboard", "Dashboard");
        }
        public IActionResult CompanyDash()
        {
            return RedirectToAction("CompanyDashboard", "Dashboard");
        }
        public IActionResult AuditDash()
        {
            return RedirectToAction("AduitDashboard", "Dashboard");
        }
    }
}
