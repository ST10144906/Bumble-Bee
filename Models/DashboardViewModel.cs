namespace BumbleBeeWebApp.Models
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserId { get; set; }
        public bool IsPartOfCompany { get; set; }
        public string CompanyId { get; set; }
    }

}
