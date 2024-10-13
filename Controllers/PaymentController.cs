using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static Google.Rpc.Context.AttributeContext.Types;

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

        [HttpGet]
        public async Task<IActionResult> Subscribe(string reference)
        {
            string secretKey = "sk_test_4dd88e062ac11594308838cd50d3bbd661730d3a"; // Paystack secret key
            string planCode = "PLN_gux61mybbcwchkz"; // Replace with the actual plan code from Paystack

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

                // Verify the transaction
                var verifyResponse = await client.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
                var verifyResult = JsonConvert.DeserializeObject<PaymentVerificationResponse>(await verifyResponse.Content.ReadAsStringAsync());

                if (verifyResponse.IsSuccessStatusCode && verifyResult.Data.Status == "success")
                {
                    // Create a subscription
                    var subscriptionData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("customer", verifyResult.Data.Customer.Email),
                        new KeyValuePair<string, string>("plan", planCode),
                        new KeyValuePair<string, string>("authorization", verifyResult.Data.Authorization.AuthorizationCode)
                    });

                    var subscriptionResponse = await client.PostAsync("https://api.paystack.co/subscription", subscriptionData);
                    var jsonString = await subscriptionResponse.Content.ReadAsStringAsync();
                    if (subscriptionResponse.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<PaymentVerificationResponse>(jsonString);
                        if (result.Data.Status == "success")
                        {
                            // Payment successfull message
                            return Content("success");
                        }
                    }
                }

                return Content("failure");
            }
        }

        // Verify the payment after Paystack callback
        [HttpGet]
        public async Task<IActionResult> VerifyPayment(string reference)
        {

            

            string secretKey = "sk_test_4dd88e062ac11594308838cd50d3bbd661730d3a";
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
                        // Payment successfull message
                        return Content("success");
                    }
                }

                // Payment failed message
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
        public PaymentCustomer Customer { get; set; }
        public PaymentAuthorization Authorization { get; set; }
    }

    public class PaymentCustomer
    {
        public string Email { get; set; } // Ensure this matches the actual JSON structure
    }

    public class PaymentAuthorization
    {
        public string AuthorizationCode { get; set; } // Ensure this matches the actual JSON structure
    }
}