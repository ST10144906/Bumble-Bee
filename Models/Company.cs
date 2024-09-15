namespace BumbleBeeWebApp.Models
{
    public class Company
    {
        public int CompanyID { get; set; }
        public string UID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public virtual ICollection<Project> Projects { get; set; }
        public virtual ICollection<CompanyDocument> CompanyDocuments { get; set; }
    }
}
