using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Apis.Auth.OAuth2;
using BumbleBeeWebApp.Models;
using Microsoft.CodeAnalysis;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.IO;



namespace BumbleBeeWebApp.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string _publicKey;
        private readonly string _secretKey;
        private readonly FirestoreDb _firestoreDb;

        public PaymentController()
        {
            var keys = JsonConvert.DeserializeObject<PaymentKeys>(System.IO.File.ReadAllText("Secrets/paymentKeys.json"));
            _publicKey = keys.public_key;
            _secretKey = keys.sk_test; // --- Use sk_live for production

            // Initialize Firestore
            var googleCredential = GoogleCredential.FromFile("Secrets/bumble-bee-foundation-firebase-adminsdk.json");

            var builder = new FirestoreClientBuilder
            {
                Credential = googleCredential
            };
            var firestoreClient = builder.Build();

            _firestoreDb = FirestoreDb.Create("bumble-bee-foundation", firestoreClient);

        }

        // --- GET: Payment page
        public async Task<IActionResult> Index(string selectedProject = null)
        {
            var projectNames = new List<string>();

            try
            {
                // Fetch all project names from Firestore
                var companiesCollection = _firestoreDb.Collection("companies");
                var companiesSnapshot = await companiesCollection.GetSnapshotAsync();

                foreach (var companyDoc in companiesSnapshot.Documents)
                {
                    var projectsCollection = companiesCollection.Document(companyDoc.Id).Collection("projects");
                    var projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                    foreach (var projectDoc in projectsSnapshot.Documents)
                    {
                        if (projectDoc.TryGetValue("ProjectName", out string projectName) &&
                            projectDoc.TryGetValue("Status", out string status) &&
                            status == "Funding Approved")
                        {
                            projectNames.Add(projectName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching projects: {ex.Message}");
            }


            var model = new PaymentViewModel
            {
                Email = "customer@example.com",
                Amount = 0,
                Name = "Customer Name",
                PublicKey = _publicKey,
                ProjectNames = projectNames,
                SelectedProject = selectedProject
            };

            return View("Payment", model);
        }

        // --- Process payment request (One-time or Recurring)
        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentViewModel model)
        {
            // Log received data for debugging
            Console.WriteLine($"Received Data:");
            Console.WriteLine($" - Email: {model.Email}");
            Console.WriteLine($" - Amount: {model.Amount}");
            Console.WriteLine($" - Name: {model.Name}");
            Console.WriteLine($" - Selected Project: {model.SelectedProject}");
            Console.WriteLine($" - Payment Type: {model.PaymentType}");
            Console.WriteLine($" - Interval: {model.Interval}");

            if (HttpContext.Session.GetString("UserType") == null)
            {
                TempData["Message"] = "You need to log in to create a donation.";
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("UserType") != "Donor")
            {
                TempData["Message"] = "You need to login as a Donor to create a donation.";
                return RedirectToAction("Login", "Account");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(model.Email))
            {
                Console.WriteLine("Error: Email is required.");
                return Json(new { error = "Email is required for payment." });
            }
            
            if(model.Email.ToString() == "customer@example.com")
            {
                Console.WriteLine("Error: Email is required.");
                return Json(new { error = "Email is required for payment." });
            }
            

            if (string.IsNullOrEmpty(model.Name))
            {
                Console.WriteLine("Error: Full Name is required.");
                return Json(new { error = "Full Name is required for payment." });
            }

            if (model.Name.ToString() == "Customer Name")
            {
                Console.WriteLine("Error: Full Name is required.");
                return Json(new { error = "Full Name is required for payment." });
            }

            if (model.Amount <= 0)
            {
                Console.WriteLine("Error: Amount must be greater than zero.");
                return Json(new { error = "Amount must be greater than zero." });
            }

            if (string.IsNullOrEmpty(model.SelectedProject))
            {
                Console.WriteLine("Error: A project must be selected.");
                return Json(new { error = "Please select a project to donate to." });
            }

            // Route to appropriate payment handler
            try
            {
                if (model.PaymentType?.ToLower() == "recurring")
                {
                    return await HandleRecurringPayment(model);
                }
                else
                {
                    return await HandleOneTimePayment(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during payment processing: {ex.Message}");
                return Json(new { error = "An unexpected error occurred while processing the payment. Please try again." });
            }
        }

        // --- Handle One-Time Payment
        private async Task<IActionResult> HandleOneTimePayment(PaymentViewModel model)
        {
            

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

                var requestData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("email", model.Email),
                    new KeyValuePair<string, string>("amount", (model.Amount * 100).ToString()), // --- amount in kobo
                    new KeyValuePair<string, string>("currency", "ZAR"),
                    new KeyValuePair<string, string>("callback_url", "https://yourdomain.com/callback"), //will have to change to http://localhost:1234
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
                        new KeyValuePair<string, string>("callback_url", "https://yourdomain.com/callback"), //will have to change to http://localhost:1234
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
        public async Task<IActionResult> VerifyPayment(string reference, string selectedProject, string fullName, string userEmail, string PaymentType, string Interval)
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
                        double amount = (double)(result.Data.Amount / 100m);

                        var donationData = new Dictionary<string, object>
                        {
                            { "ProjectName", selectedProject },
                            { "FullName", fullName },
                            { "UserEmail", userEmail },
                            { "Amount", amount },
                            { "Timestamp", DateTime.UtcNow },
                            { "PaymentType", PaymentType}, 
                            { "PaymentInterval", Interval}
                        };

                        try
                        {
                            var donationsCollection = _firestoreDb.Collection("donations");
                            await donationsCollection.AddAsync(donationData);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error inserting into Firestore: {ex.Message}");
                            return Content("Payment verification successful, but failed to log donation.");
                        }

                        //return Content("Payment verification successful and donation logged.");
                        return RedirectToAction("CreateInvoice", new
                        {
                            projectName = selectedProject,
                            fullName = fullName,
                            email = userEmail,
                            amount = amount,
                            time = DateTime.UtcNow,
                            paymentType = PaymentType,
                            interval = Interval
                        });
                    }
                }
                return Content("Payment verification failed");
            }
        }
        [HttpGet]
        public async Task<IActionResult> CreateInvoice(
            string projectName,
            string fullName,
            string email,
            double amount,
            DateTime time,
            string paymentType,
            string interval)
        {
            try
            {
                // Query Firestore to find user information by email
                var usersCollection = _firestoreDb.Collection("users");
                var query = usersCollection.WhereEqualTo("Email", email);
                var querySnapshot = await query.GetSnapshotAsync();

                if (!querySnapshot.Documents.Any())
                {
                    return Content("No user found with the specified email.");
                }

                // Extract user information
                var userDocument = querySnapshot.Documents.First();
                var userData = userDocument.ToDictionary();

                string userEmail = userData["Email"].ToString();
                string userFullName = userData["FullName"].ToString();
                string idNumber = userData.ContainsKey("IdNumber") ? userData["IdNumber"].ToString() : "N/A";
                string phoneNumber = userData.ContainsKey("PhoneNumber") ? userData["PhoneNumber"].ToString() : "N/A";
                string taxNumber = userData.ContainsKey("TaxNumber") ? userData["TaxNumber"].ToString() : "N/A";
                string userType = userData.ContainsKey("Type") ? userData["Type"].ToString() : "N/A";
                string uid = userData["Uid"].ToString();

                // Generate PDF invoice
                var pdfBytes = GenerateInvoicePdf(
                    projectName, fullName, email, amount, time, paymentType, interval,
                    userEmail, userFullName, idNumber, phoneNumber, taxNumber, userType, uid
                );

                // Return the PDF as a downloadable file
                return File(pdfBytes, "application/pdf", "Invoice.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating invoice: {ex.Message}");
                return Content("An error occurred while creating the invoice.");
            }
        }

        private byte[] GenerateInvoicePdf(
            string projectName,
            string fullName,
            string email,
            double amount,
            DateTime time,
            string paymentType,
            string interval,
            string userEmail,
            string userFullName,
            string idNumber,
            string phoneNumber,
            string taxNumber,
            string userType,
            string uid)
        {
            try
            {
                Console.WriteLine("Creating PDF document...");
                
                // Create a new PDF document
                PdfDocument document = new PdfDocument();
                document.Info.Title = "Invoice";

                // Add a page to the document
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Define fonts
                XFont headerFont = new XFont("Times New Roman", 16);
                XFont regularFont = new XFont("Times New Roman", 12);

                // Draw header
                gfx.DrawString("Invoice", headerFont, XBrushes.Black, new XRect(0, 0, page.Width, page.Height), XStringFormats.TopCenter);

                // Draw invoice details
                gfx.DrawString($"Date: {time:yyyy-MM-dd HH:mm:ss}", regularFont, XBrushes.Black, new XPoint(40, 100));
                gfx.DrawString($"Project Name: {projectName}", regularFont, XBrushes.Black, new XPoint(40, 130));
                gfx.DrawString($"Amount: {amount:C}", regularFont, XBrushes.Black, new XPoint(40, 160));
                gfx.DrawString($"Payment Type: {paymentType}", regularFont, XBrushes.Black, new XPoint(40, 190));
                gfx.DrawString($"Payment Interval: {interval ?? "N/A"}", regularFont, XBrushes.Black, new XPoint(40, 220));

                // Draw user details
                gfx.DrawString("User Details", headerFont, XBrushes.Black, new XPoint(40, 260));
                gfx.DrawString($"Full Name: {userFullName}", regularFont, XBrushes.Black, new XPoint(40, 290));
                gfx.DrawString($"Email: {userEmail}", regularFont, XBrushes.Black, new XPoint(40, 320));
                gfx.DrawString($"ID Number: {idNumber}", regularFont, XBrushes.Black, new XPoint(40, 350));
                gfx.DrawString($"Phone Number: {phoneNumber}", regularFont, XBrushes.Black, new XPoint(40, 380));
                gfx.DrawString($"Tax Number: {taxNumber}", regularFont, XBrushes.Black, new XPoint(40, 410));
                gfx.DrawString($"User Type: {userType}", regularFont, XBrushes.Black, new XPoint(40, 440));
                gfx.DrawString($"UID: {uid}", regularFont, XBrushes.Black, new XPoint(40, 470));

                // Save to MemoryStream
                Console.WriteLine("Saving PDF to MemoryStream...");
                using (var memoryStream = new MemoryStream())
                {
                    document.Save(memoryStream, false); // Save without closing the stream
                    Console.WriteLine("PDF saved successfully.");

                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Array.Empty<byte>();
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

        
        [JsonProperty("ProjectNames")]
        public List<string> ProjectNames { get; set; }

        [JsonProperty("SelectedProject")]
        public string SelectedProject { get; set; }
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