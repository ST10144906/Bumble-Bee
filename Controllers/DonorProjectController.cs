using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBeeWebApp.Models;

public class DonorProjectController : Controller
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<DonorProjectController> _logger;

    public DonorProjectController(FirestoreDb firestoreDb, ILogger<DonorProjectController> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    // GET: DonorProject/ViewAllProjects
    public async Task<IActionResult> ViewAllProjects()
    {
        List<(string CompanyName, Project Project)> allProjects = new List<(string, Project)>();

        try
        {
            // Retrieve all documents from the "companies" collection
            CollectionReference companiesCollection = _firestoreDb.Collection("companies");
            QuerySnapshot companiesSnapshot = await companiesCollection.GetSnapshotAsync();

            foreach (DocumentSnapshot companyDoc in companiesSnapshot.Documents)
            {
                if (companyDoc.Exists)
                {
                    string companyName = companyDoc.ContainsField("Name") ? companyDoc.GetValue<string>("Name") : "Unknown Company";

                    // Get the "projects" subcollection for each company
                    CollectionReference projectsCollection = companyDoc.Reference.Collection("projects");
                    QuerySnapshot projectsSnapshot = await projectsCollection.GetSnapshotAsync();

                    // Add each project document to the allProjects list
                    foreach (DocumentSnapshot projectDoc in projectsSnapshot.Documents)
                    {
                        if (projectDoc.Exists)
                        {
                            try
                            {
                                string status = projectDoc.ContainsField("Status") ? projectDoc.GetValue<string>("Status") : null;

                                if (status == "Funding Approved")
                                {
                                    var project = new Project
                                    {
                                        ProjectName = projectDoc.ContainsField("ProjectName") ? projectDoc.GetValue<string>("ProjectName") : "Unknown Project",
                                        Description = projectDoc.ContainsField("Description") ? projectDoc.GetValue<string>("Description") : "No Description",
                                        DateCreated = projectDoc.ContainsField("DateCreated") ? projectDoc.GetValue<DateTime>("DateCreated") : DateTime.MinValue,
                                        MiscellaneousDocumentsUrl = projectDoc.ContainsField("MiscellaneousDocumentsUrl") ? projectDoc.GetValue<string>("MiscellaneousDocumentsUrl") : null
                                    };

                                    allProjects.Add((companyName, project));
                                }
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogError(innerEx, $"Error processing project document ID: {projectDoc.Id}");
                            }
                        }
                    }
                }
            }

            if (allProjects.Count == 0)
            {
                ViewBag.NoProjectsMessage = "No projects found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects from Firestore.");
            ViewBag.NoProjectsMessage = "An error occurred while retrieving the projects.";
        }

        return View(allProjects);
    }
}
