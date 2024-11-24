using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using static Google.Api.Gax.Grpc.Gcp.AffinityConfig.Types;
using static System.Collections.Specialized.BitVector32;

public class AuditController : Controller
{
    private readonly FirestoreDb _firestoreDb;

    public AuditController(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    // Action for auditing donations and logging them
    [HttpPost]
    public async Task<IActionResult> AuditTransaction(string donationId)
    {
        var userId = HttpContext.Session.GetString("FirebaseUserId") ?? "UnknownUser";

        try
        {
            // Check if DonationId exists in auditLogs
            Query existingAuditQuery = _firestoreDb.Collection("auditLogs").WhereEqualTo("DonationId", donationId);
            QuerySnapshot existingAuditSnapshot = await existingAuditQuery.GetSnapshotAsync();

            if (existingAuditSnapshot.Documents.Any())
            {
                TempData["ErrorMessage"] = "This donation has already been audited.";
                return RedirectToAction("AuditTransactionsView");
            }

            // Query Firestore for the specific donation
            DocumentSnapshot donationSnapshot = await _firestoreDb.Collection("donations").Document(donationId).GetSnapshotAsync();

            if (!donationSnapshot.Exists)
            {
                TempData["ErrorMessage"] = "The specified donation could not be found.";
                return RedirectToAction("AuditTransactionsView");
            }

            var donation = donationSnapshot.ToDictionary();

            // Create an AuditLog entry
            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid().ToString(),
                DonationId = donationId,
                ProjectName = donation["ProjectName"].ToString(),
                UserId = userId,
                Amount = Convert.ToDouble(donation["Amount"]),
                PaymentType = donation["PaymentType"].ToString(),
                AuditedAt = DateTime.UtcNow,
                Notes = "Transaction reviewed and verified."
            };

            await _firestoreDb.Collection("auditLogs").Document(auditLog.AuditLogId).SetAsync(new Dictionary<string, object>
        {
            { "AuditLogId", auditLog.AuditLogId },
            { "DonationId", auditLog.DonationId },
            { "ProjectName", auditLog.ProjectName },
            { "UserId", auditLog.UserId },
            { "Amount", auditLog.Amount },
            { "PaymentType", auditLog.PaymentType },
            { "AuditedAt", auditLog.AuditedAt },
            { "Notes", auditLog.Notes }
        });

            TempData["SuccessMessage"] = "Transaction audited successfully.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error auditing donation: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while auditing the transaction.";
        }

        return RedirectToAction("AuditTransactionsView");
    }
   

    [HttpGet]
    public async Task<IActionResult> AuditTransactionsView()
    {
        var donations = new List<Dictionary<string, object>>();

        try
        {
            // Query Firestore for all donations
            Query donationsQuery = _firestoreDb.Collection("donations");
            QuerySnapshot donationsSnapshot = await donationsQuery.GetSnapshotAsync();

            // Query Firestore for all audited donations
            Query auditLogsQuery = _firestoreDb.Collection("auditLogs");
            QuerySnapshot auditLogsSnapshot = await auditLogsQuery.GetSnapshotAsync();

            var auditedDonationIds = new HashSet<string>();
            foreach (var auditLog in auditLogsSnapshot.Documents)
            {
                auditedDonationIds.Add(auditLog.GetValue<string>("DonationId"));
            }

            foreach (var document in donationsSnapshot.Documents)
            {
                var donation = document.ToDictionary();
                donation["DonationId"] = document.Id;

                // Skip donations that have already been audited
                if (auditedDonationIds.Contains(document.Id))
                {
                    continue;
                }

                donations.Add(donation);
            }

            if (!donations.Any())
            {
                TempData["ErrorMessage"] = "No donations found to audit.";
                return RedirectToAction("Dashboard", "Dashboard");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving donations: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while retrieving donations.";
            return RedirectToAction("Dashboard", "Dashboard");
        }

        // Pass the filtered donations to the view
        return View("AuditTransactions", donations); 
    }

    [HttpGet]
    public async Task<IActionResult> AuditLogsView()
    {
        var auditLogs = new List<AuditLog>();

        try
        {
            Query auditLogsQuery = _firestoreDb.Collection("auditLogs");
            QuerySnapshot auditLogsSnapshot = await auditLogsQuery.GetSnapshotAsync();

            foreach (var document in auditLogsSnapshot.Documents)
            {
                var data = document.ToDictionary();

                var auditLog = new AuditLog
                {
                    AuditLogId = document.Id,
                    ProjectName = data.ContainsKey("ProjectName") ? data["ProjectName"].ToString() : "Unknown",
                    Amount = data.ContainsKey("Amount") ? Convert.ToDouble(data["Amount"]) : 0,
                    PaymentType = data.ContainsKey("PaymentType") ? data["PaymentType"].ToString() : "N/A",
                    AuditedAt = data.ContainsKey("AuditedAt") ? ((Google.Cloud.Firestore.Timestamp)data["AuditedAt"]).ToDateTime() : DateTime.MinValue,
                    Notes = data.ContainsKey("Notes") ? data["Notes"].ToString() : "No notes"
                };

                auditLogs.Add(auditLog);
            }

            if (!auditLogs.Any())
            {
                TempData["ErrorMessage"] = "No audit logs found.";
                return RedirectToAction("Dashboard", "Dashboard");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving audit logs: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while retrieving audit logs.";
            return RedirectToAction("Dashboard", "Dashboard");
        }

        return View("AuditLogs", auditLogs);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateSelectedReports(List<string> selectedAuditLogIds)
    {
        var selectedAuditLogs = new List<AuditLog>();

        try
        {
            foreach (var logId in selectedAuditLogIds)
            {
                DocumentSnapshot auditLogSnapshot = await _firestoreDb.Collection("auditLogs").Document(logId).GetSnapshotAsync();

                if (auditLogSnapshot.Exists)
                {
                    var data = auditLogSnapshot.ToDictionary();

                    var auditLog = new AuditLog
                    {
                        AuditLogId = auditLogSnapshot.Id,
                        ProjectName = data.ContainsKey("ProjectName") ? data["ProjectName"].ToString() : "Unknown",
                        Amount = data.ContainsKey("Amount") ? Convert.ToDouble(data["Amount"]) : 0,
                        PaymentType = data.ContainsKey("PaymentType") ? data["PaymentType"].ToString() : "N/A",
                        AuditedAt = data.ContainsKey("AuditedAt") ? ((Google.Cloud.Firestore.Timestamp)data["AuditedAt"]).ToDateTime() : DateTime.MinValue,
                        Notes = data.ContainsKey("Notes") ? data["Notes"].ToString() : "No notes"
                    };

                    selectedAuditLogs.Add(auditLog);
                }
            }

            // Generate the PDF report for selected logs
            var pdfBytes = GenerateAuditReportPdf(selectedAuditLogs);

            // Return PDF file
            return File(pdfBytes, "application/pdf", "SelectedAuditReport.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating selected audit report: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while generating the selected audit report.";
            return RedirectToAction("AuditLogsView");
        }
    }

    // Action to generate audit report PDF
    [HttpPost]
    public async Task<IActionResult> GenerateAuditReports()
    {
        var auditLogs = new List<AuditLog>();

        try
        {
            Console.WriteLine("Querying Firestore for audit logs...");
            Query auditLogsQuery = _firestoreDb.Collection("auditLogs");
            QuerySnapshot auditLogsSnapshot = await auditLogsQuery.GetSnapshotAsync();

            Console.WriteLine($"Found {auditLogsSnapshot.Documents.Count} audit logs.");

            if (!auditLogsSnapshot.Documents.Any())
            {
                Console.WriteLine("No audit logs found.");
                TempData["ErrorMessage"] = "No audit logs available to generate the report.";
                return RedirectToAction("Dashboard", "Dashboard");
            }

            foreach (var document in auditLogsSnapshot.Documents)
            {
                var data = document.ToDictionary();

                var auditLog = new AuditLog
                {
                    AuditLogId = document.Id,
                    ProjectName = data.ContainsKey("ProjectName") ? data["ProjectName"].ToString() : "Unknown",
                    Amount = data.ContainsKey("Amount") ? Convert.ToDouble(data["Amount"]) : 0,
                    PaymentType = data.ContainsKey("PaymentType") ? data["PaymentType"].ToString() : "N/A",
                    AuditedAt = data.ContainsKey("AuditedAt") ? ((Timestamp)data["AuditedAt"]).ToDateTime() : DateTime.MinValue,
                    Notes = data.ContainsKey("Notes") ? data["Notes"].ToString() : "No notes"
                };

                auditLogs.Add(auditLog);
                Console.WriteLine($"Audit Log Processed: {auditLog.ProjectName}, {auditLog.Amount}, {auditLog.PaymentType}, {auditLog.AuditedAt}");
            }

            //foreach (var document in auditLogsSnapshot.Documents)
            //{
            //    try
            //    {
            //        var auditLog = document.ConvertTo<AuditLog>();
            //        auditLogs.Add(auditLog);
            //        Console.WriteLine($"Audit Log Retrieved: {auditLog.AuditLogId}, {auditLog.ProjectName}, {auditLog.Amount}, {auditLog.PaymentType}, {auditLog.AuditedAt}");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"Error converting document {document.Id}: {ex.Message}");
            //    }
            //}

            Console.WriteLine("Generating PDF report...");
            var pdfBytes = GenerateAuditReportPdf(auditLogs);

            Console.WriteLine("Returning PDF as downloadable file...");
            return File(pdfBytes, "application/pdf", "AuditReport.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating audit report: {ex.Message}");
            TempData["ErrorMessage"] = $"An error occurred while generating the audit report: {ex.Message}";
            return RedirectToAction("Dashboard", "Dashboard");
        }
    }

   

    private byte[] GenerateAuditReportPdf(List<AuditLog> auditLogs)
    {
        try
        {
            Console.WriteLine("Creating PDF document...");
            PdfDocument document = new PdfDocument();
            document.Info.Title = "Audit Report";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Load the logo image
            string logoPath = "wwwroot/images/logo.png"; 
            
                XImage logo = XImage.FromFile(logoPath);

                // Center the logo horizontally at the top of the page
                double logoWidth = 100; 
                double logoHeight = logo.PixelHeight * logoWidth / logo.PixelWidth; 
                double logoX = (page.Width - logoWidth) / 2; 
                double logoY = 20; 

                // Draw the logo
                gfx.DrawImage(logo, logoX, logoY, logoWidth, logoHeight);

            double tableStartX = 40;
            double tableStartY = 140; 
            double tableWidth = page.Width - 80;
            double tableRowHeight = 25;
            double currentY = tableStartY;

            


            // Fonts
            XFont titleFont = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
            XFont boldFont = new XFont("Times New Roman", 12, XFontStyleEx.Bold);
            XFont regularFont = new XFont("Times New Roman", 12);
            XFont footerFont = new XFont("Times New Roman", 10, XFontStyleEx.Italic);
            
            // Draw Title
            gfx.DrawString("BumbleBee Audit Report", titleFont, XBrushes.Black,
                new XRect(0, 100, page.Width, 40), XStringFormats.TopCenter); 

            // Table Setup
            

            gfx.DrawRectangle(XPens.Black, tableStartX, tableStartY, tableWidth, tableRowHeight * (auditLogs.Count + 1));

            // Table Header Row
            
            gfx.DrawString("Project Name", boldFont, XBrushes.Black, new XPoint(tableStartX + 5, currentY + 15));
            gfx.DrawString("Amount", boldFont, XBrushes.Black, new XPoint(tableStartX + 200, currentY + 15));
            gfx.DrawString("Payment Type", boldFont, XBrushes.Black, new XPoint(tableStartX + 300, currentY + 15));
            gfx.DrawString("Audited At", boldFont, XBrushes.Black, new XPoint(tableStartX + 400, currentY + 15));

            gfx.DrawLine(XPens.Black, tableStartX, currentY + tableRowHeight, tableStartX + tableWidth, currentY + tableRowHeight);

            // Add the rows of the audit logs
            currentY += tableRowHeight;
            foreach (var log in auditLogs)
            {
                try
                {
                    gfx.DrawString(log.ProjectName ?? "N/A", regularFont, XBrushes.Black, new XPoint(tableStartX + 5, currentY + 15));
                    gfx.DrawString(log.Amount.ToString("C"), regularFont, XBrushes.Black, new XPoint(tableStartX + 200, currentY + 15));
                    gfx.DrawString(log.PaymentType ?? "N/A", regularFont, XBrushes.Black, new XPoint(tableStartX + 300, currentY + 15));
                    gfx.DrawString(log.AuditedAt.ToString("yyyy-MM-dd HH:mm:ss"), regularFont, XBrushes.Black, new XPoint(tableStartX + 400, currentY + 15));
                    currentY += tableRowHeight;

                    // Draw line between rows
                    gfx.DrawLine(XPens.Black, tableStartX, currentY, tableStartX + tableWidth, currentY);

                    // Check if the content exceeds the page height
                    if (currentY > page.Height - 50)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        currentY = 40;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error rendering log {log.AuditLogId}: {ex.Message}");
                }
            }

            // Footer Section with Auditor's name and date
            currentY += 40;
            string userFullName = HttpContext.Session.GetString("UserFullName");

            gfx.DrawString("I confirm that the above transactions were audited for the BumbleBee Foundation.",
                regularFont, XBrushes.Black,
                new XRect(40, currentY, page.Width - 80, 20), XStringFormats.Center);

            currentY += 55;
            double nameWidth = 200;
            double lineLength = nameWidth + 30;
            double lineStartX = (page.Width - lineLength) / 2;
            gfx.DrawLine(XPens.Black, lineStartX, currentY, lineStartX + lineLength, currentY);
            gfx.DrawString(userFullName, boldFont, XBrushes.Black, new XRect(lineStartX, currentY - 20, lineLength, 20), XStringFormats.Center);
            gfx.DrawString("Auditor", footerFont, XBrushes.Black, new XRect(lineStartX, currentY + 10, lineLength, 20), XStringFormats.Center);

            currentY += 50;
            gfx.DrawString("Date of Issue: " + DateTime.UtcNow.ToString("yyyy-MM-dd"),
                footerFont, XBrushes.Black, new XRect((page.Width - 200) / 2, currentY, 200, 20), XStringFormats.TopCenter);

            Console.WriteLine("Saving PDF to MemoryStream...");
            using (var memoryStream = new MemoryStream())
            {
                document.Save(memoryStream, false);
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating PDF: {ex.Message}");
            return Array.Empty<byte>();
        }
    }



    [HttpGet]
    public async Task<IActionResult> SearchDonations(string searchType, string searchTerm)
    {
        var donations = new List<Dictionary<string, object>>();

        try
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                TempData["ErrorMessage"] = "Please enter a search term.";
                return RedirectToAction("AuditTransactionsView");
            }

            // Convert search term to lowercase
            searchTerm = searchTerm.ToLower();

            Query donationsQuery = _firestoreDb.Collection("donations");

            QuerySnapshot donationsSnapshot = await donationsQuery.GetSnapshotAsync();

            foreach (var document in donationsSnapshot.Documents)
            {
                var donation = document.ToDictionary();
                donation["DonationId"] = document.Id; 

                // Check if the field matches the search term
                if (searchType == "ProjectName" && donation.ContainsKey("ProjectName") &&
                    donation["ProjectName"].ToString().ToLower().Contains(searchTerm))
                {
                    donations.Add(donation);
                }
                else if (searchType == "DonorEmail" && donation.ContainsKey("UserEmail") &&
                         donation["UserEmail"].ToString().ToLower().Contains(searchTerm))
                {
                    donations.Add(donation);
                }
            }

            if (!donations.Any())
            {
                TempData["ErrorMessage"] = "No donations matched your search.";
                return RedirectToAction("AuditTransactionsView");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching donations: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while searching donations.";
            return RedirectToAction("AuditTransactionsView");
        }

        return View("AuditTransactions", donations); // Reuse the same view
    }

    [HttpGet]
    public async Task<IActionResult> SearchAuditLogs(string searchType, string searchTerm)
    {
        var auditLogs = new List<AuditLog>();

        try
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                TempData["ErrorMessage"] = "Please enter a search term.";
                return RedirectToAction("AuditLogsView");
            }

            // Convert the search term to lowercase for case-insensitive matching
            searchTerm = searchTerm.ToLower();

            // Query Firestore for all AuditLogs
            Query auditLogsQuery = _firestoreDb.Collection("auditLogs");
            QuerySnapshot auditLogsSnapshot = await auditLogsQuery.GetSnapshotAsync();

            foreach (var document in auditLogsSnapshot.Documents)
            {
                var data = document.ToDictionary();

                // Convert the document to an AuditLog model
                var auditLog = new AuditLog
                {
                    AuditLogId = document.Id,
                    ProjectName = data.ContainsKey("ProjectName") ? data["ProjectName"].ToString() : string.Empty,
                    Amount = data.ContainsKey("Amount") ? Convert.ToDouble(data["Amount"]) : 0,
                    PaymentType = data.ContainsKey("PaymentType") ? data["PaymentType"].ToString() : string.Empty,
                    AuditedAt = data.ContainsKey("AuditedAt") ? ((Google.Cloud.Firestore.Timestamp)data["AuditedAt"]).ToDateTime() : DateTime.MinValue,
                    Notes = data.ContainsKey("Notes") ? data["Notes"].ToString() : string.Empty
                };

                // Check if the field matches the search term
                if (searchType == "ProjectName" && auditLog.ProjectName.ToLower().Contains(searchTerm))
                {
                    auditLogs.Add(auditLog);
                }
                else if (searchType == "PaymentType" && auditLog.PaymentType.ToLower().Contains(searchTerm))
                {
                    auditLogs.Add(auditLog);
                }
            }

            if (!auditLogs.Any())
            {
                TempData["ErrorMessage"] = "No audit logs matched your search.";
                return RedirectToAction("AuditLogsView");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching audit logs: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while searching audit logs.";
            return RedirectToAction("AuditLogsView");
        }

        // Return the filtered AuditLogs to the view
        return View("AuditLogs", auditLogs);
    }
}

public static class StringExtensions
{
    public static string ToTitleCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
        var textInfo = cultureInfo.TextInfo;
        return textInfo.ToTitleCase(str.ToLower());
    }
}
