namespace BumbleBeeWebApp.Models
{
    public class Project
    {
        public int CompanyID { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "Pending Approval";
        public IFormFile MiscellaneousDocuments { get; set; }
        public string MiscellaneousDocumentsUrl { get; set; } = string.Empty;
    }
}
