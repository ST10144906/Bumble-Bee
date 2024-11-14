using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BumbleBeeWebApp.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string _publicKey;
        private readonly string _secretKey;

        public PaymentController()
        {
            var keys = JsonConvert.DeserializeObject<PaymentKeys>(System.IO.File.ReadAllText("Secrets/paymentKeys.json"));
            _publicKey = keys.public_key;
            _secretKey = keys.sk_test; // --- Use sk_live for production
        }

        // --- GET: Payment page
        public ActionResult Index()
        {
            var model = new PaymentViewModel
            {
                Email = "customer@example.com",
                Amount = 5000,
                Name = "Customer Name",
                PublicKey = _publicKey
            };
            return View("Payment", model);
        }

        // --- Process payment request (One-time or Recurring)
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            if (model.PaymentType == "recurring")
            {
                return await HandleRecurringPayment(model);
            }
            else
            {
                return await HandleOneTimePayment(model);
            }
        }

        // --- Handle One-Time Payment
        private async Task<IActionResult> HandleOneTimePayment(PaymentViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return Json(new { error = "Email is required for payment." });
            }
            if (model.Amount <= 0)
            {
                return Json(new { error = "Amount must be greater than zero." });
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

                var requestData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("email", model.Email),
                    new KeyValuePair<string, string>("amount", (model.Amount * 100).ToString()), // --- amount in kobo
                    new KeyValuePair<string, string>("currency", "ZAR"),
                    new KeyValuePair<string, string>("callback_url", "https://yourdomain.com/callback"),
                    new KeyValuePair<string, string>("reference", Guid.NewGuid().ToString())
                });

                var response = await client.PostAsync("https://api.paystack.co/transaction/initialize", requestData);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PaystackInitResponse>(jsonResponse);
                    if (!string.IsNullOrEmpty(result?.Data?.AuthorizationUrl))
                    {
                        return Json(new { authorizationUrl = result.Data.AuthorizationUrl, reference = result.Data.Reference });
                    }
                    else
                    {
                        Console.WriteLine("Authorization URL is null or empty.");
                        return Json(new { error = "Failed to get authorization URL from Paystack." });
                    }
                }
                else
                {
                    Console.WriteLine($"Paystack API Error: {jsonResponse}");
                    return Json(new { error = "Failed to initialize payment. Check Paystack API response." });
                }
            }
        }

        // --- Handle Recurring Payment by creating a subscription plan
        private async Task<IActionResult> HandleRecurringPayment(PaymentViewModel model)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

                // --- Create the plan
                var planData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("name", $"Subscription Plan - {model.Interval}"),
                    new KeyValuePair<string, string>("amount", (model.Amount * 100).ToString()),
                    new KeyValuePair<string, string>("interval", model.Interval)
                });

                var planResponse = await client.PostAsync("https://api.paystack.co/plan", planData);
                var jsonResponse = await planResponse.Content.ReadAsStringAsync();

                if (planResponse.IsSuccessStatusCode)
                {
                    var planResult = JsonConvert.DeserializeObject<PlanResponse>(jsonResponse);
                    var planCode = planResult.Data.PlanCode;

                    // --- Initialize transaction for initial payment and retrieve authorization URL
                    var transactionData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("email", model.Email),
                        new KeyValuePair<string, string>("amount", (model.Amount * 100).ToString()),
                        new KeyValuePair<string, string>("currency", "ZAR"),
                        new KeyValuePair<string, string>("plan", planCode),
                        new KeyValuePair<string, string>("callback_url", "https://yourdomain.com/callback"),
                        new KeyValuePair<string, string>("reference", Guid.NewGuid().ToString())
                    });

                    var transactionResponse = await client.PostAsync("https://api.paystack.co/transaction/initialize", transactionData);
                    var transactionJson = await transactionResponse.Content.ReadAsStringAsync();

                    if (transactionResponse.IsSuccessStatusCode)
                    {
                        var transactionResult = JsonConvert.DeserializeObject<PaystackInitResponse>(transactionJson);
                        return Json(new { authorizationUrl = transactionResult.Data.AuthorizationUrl, reference = transactionResult.Data.Reference });
                    }
                    Console.WriteLine($"Transaction Error: {transactionJson}");
                    return Json(new { error = "Failed to initialize transaction for recurring payment." });
                }
                Console.WriteLine($"Plan Creation Error: {jsonResponse}");
                return Json(new { error = "Failed to create recurring plan. Check Paystack API response." });
            }
        }

        

        // --- Cancel a recurring payment subscription
        [HttpPost]
        public async Task<IActionResult> CancelRecurringPayment(string subscriptionCode)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
                var response = await client.PostAsync($"https://api.paystack.co/subscription/{subscriptionCode}/disable", null);

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Subscription canceled successfully");
                }
                return BadRequest("Failed to cancel subscription");
            }
        }

        // --- Fetch invoices for the logged-in user
        [HttpGet]
        public async Task<IActionResult> FetchInvoices(string customerEmail)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
                var response = await client.GetAsync($"https://api.paystack.co/transaction?customer={customerEmail}");
                var jsonString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var invoices = JsonConvert.DeserializeObject<InvoiceResponse>(jsonString);
                    return Ok(invoices);
                }
                return BadRequest("Failed to fetch invoices");
            }
        }

        // --- Verify the payment after Paystack callback
        [HttpGet]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
                var response = await client.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
                var jsonString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PaymentVerificationResponse>(jsonString);
                    if (result.Data.Status == "success")
                    {
                        return Content("Payment verification successful");
                    }
                }
                return Content("Payment verification failed");
            }
        }
    }

    // --- Model classes for deserializing JSON responses
    public class PaymentViewModel
    {
        [JsonProperty("Email")]
        public string Email { get; set; }
        
        [JsonProperty("Amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("Name")]
        public string Name { get; set; }
        
        [JsonProperty("PublicKey")]
        public string PublicKey { get; set; }
        
        [JsonProperty("PaymentType")]
        public string PaymentType { get; set; }
        
        [JsonProperty("Interval")]
        public string Interval { get; set; }
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
        public string Email { get; set; }
    }

    public class PaymentAuthorization
    {
        public string AuthorizationCode { get; set; }
    }

    public class PlanResponse
    {
        public string Status { get; set; }
        public PlanData Data { get; set; }
    }

    public class PlanData
    {
        [JsonProperty("plan_code")] 
        public string PlanCode { get; set; }
    }

    public class InvoiceResponse
    {
        public string Status { get; set; }
        public List<InvoiceData> Data { get; set; }
    }

    public class InvoiceData
    {
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime PaidAt { get; set; }
    }

    public class PaymentKeys
    {
        public string public_key { get; set; }
        public string sk_test { get; set; }
        public string sk_live { get; set; }
    }

    public class PaystackInitResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public PaystackData Data { get; set; }
    }

    public class PaystackData
    {
        [JsonProperty("authorization_url")]
        public string AuthorizationUrl { get; set; }

        [JsonProperty("access_code")]
        public string AccessCode { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }
    }
}
