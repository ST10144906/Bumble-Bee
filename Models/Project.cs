namespace BumbleBeeWebApp.Models
{
    public class Project
    {
        public int ProjectID { get; set; }
        public int CompanyID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal FundingTarget { get; set; }
        public string Status { get; set; }

        public virtual Company Company { get; set; }
        public virtual ICollection<FundingRequest> FundingRequests { get; set; }
        public virtual ICollection<Donation> Donations { get; set; }
        public virtual ICollection<ProjectDocuments> ProjectDocuments { get; set; }
    }
}
