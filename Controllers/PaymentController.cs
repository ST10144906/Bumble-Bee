using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers
{
    public class PaymentController : Controller
    {
        // GET: PaymentController
        public ActionResult Index()
        {
            var model = new PaymentViewModel
            {
                Email = "customer@example.com",
                Amount = 5000,
                Name = "Customer Name",
                PublicKey = "pk_test_a9de9913fcfaa616dbb09598918c2edc2dd20581"
            };

            return View("Payment", model);
        }

        // Verify the payment after Paystack callback
        [HttpGet]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            string secretKey = "sk_test_4dd88e062ac11594308838cd50d3bbd661730d3a"; // Replace with your secret key
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
                var response = await client.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
                var jsonString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PaymentVerificationResponse>(jsonString);
                    if (result.Data.Status == "success")
                    {
                        // Payment successful, return success to frontend
                        return Content("success");
                    }
                }

                // Payment failed, return failure to frontend
                return Content("failure");
            }
        }
    }

    public class PaymentViewModel
    {
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; }
        public string PublicKey { get; set; }
    }

    public class PaymentVerificationResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public Data Data { get; set; }
    }

    public class Data
    {
        public string Status { get; set; }
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string GatewayResponse { get; set; }
    }
}