using BumbleBeeWebApp.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using static Google.Api.Gax.Grpc.Gcp.AffinityConfig.Types;

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

        return View("AuditTransactions", donations); // Pass the filtered donations to the view
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

            XFont headerFont = new XFont("Times New Roman", 16);
            XFont regularFont = new XFont("Times New Roman", 12);

            // Draw Header
            gfx.DrawString("Audit Report", headerFont, XBrushes.Black, new XRect(0, 0, page.Width, page.Height), XStringFormats.TopCenter);
            gfx.DrawString($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", regularFont, XBrushes.Black, new XPoint(40, 50));

            // Table Header
            int y = 100;
            gfx.DrawString("Project Name", regularFont, XBrushes.Black, new XPoint(40, y));
            gfx.DrawString("Amount", regularFont, XBrushes.Black, new XPoint(200, y));
            gfx.DrawString("Payment Type", regularFont, XBrushes.Black, new XPoint(300, y));
            gfx.DrawString("Audited At", regularFont, XBrushes.Black, new XPoint(400, y));

            y += 20;

            // Table Data
            foreach (var log in auditLogs)
            {
                try
                {
                    gfx.DrawString(log.ProjectName ?? "N/A", regularFont, XBrushes.Black, new XPoint(40, y));
                    gfx.DrawString(log.Amount.ToString("C"), regularFont, XBrushes.Black, new XPoint(200, y));
                    gfx.DrawString(log.PaymentType ?? "N/A", regularFont, XBrushes.Black, new XPoint(300, y));
                    gfx.DrawString(log.AuditedAt != DateTime.MinValue ? log.AuditedAt.ToString("yyyy-MM-dd HH:mm:ss") : "N/A", regularFont, XBrushes.Black, new XPoint(400, y));
                    y += 20;

                    if (y > page.Height - 50) // Add a new page if needed
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 50;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error rendering log {log.AuditLogId}: {ex.Message}");
                }
            }

            //foreach (var log in auditLogs)
            //{
            //    try
            //    {
            //        gfx.DrawString(log.ProjectName, regularFont, XBrushes.Black, new XPoint(40, y));
            //        gfx.DrawString(log.Amount.ToString("C"), regularFont, XBrushes.Black, new XPoint(200, y));
            //        gfx.DrawString(log.PaymentType, regularFont, XBrushes.Black, new XPoint(300, y));
            //        gfx.DrawString(log.AuditedAt.ToString("yyyy-MM-dd HH:mm:ss"), regularFont, XBrushes.Black, new XPoint(400, y));
            //        y += 20;

            //        if (y > page.Height - 50) // Add a new page if needed
            //        {
            //            Console.WriteLine("Adding new page...");
            //            page = document.AddPage();
            //            gfx = XGraphics.FromPdfPage(page);
            //            y = 50;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"Error rendering log {log.AuditLogId}: {ex.Message}");
            //    }
            //}

            Console.WriteLine("Saving PDF to memory stream...");
            using (var memoryStream = new MemoryStream())
            {
                document.Save(memoryStream, false); // Save without closing the stream
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating PDF: {ex.Message}");
            return Array.Empty<byte>();
        }
    }
}
