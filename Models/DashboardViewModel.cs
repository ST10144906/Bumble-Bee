namespace BumbleBeeWebApp.Models
{
    public class DashboardViewModel
    {
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Project> FundingApprovedProjects { get; set; } = new List<Project>();
        public List<Project> PendingApprovalProjects { get; set; } = new List<Project>();
        public string UserType { get; set; }
    }

}
