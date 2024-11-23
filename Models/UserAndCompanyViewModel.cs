namespace BumbleBeeWebApp.Models
{
    public class UserAndCompanyViewModel
    {
        // User Details
        public string UserEmail { get; set; }
        public string UserFullName { get; set; }
        public string UserJobRole { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserType { get; set; }

        // Company Details
        public string CompanyName { get; set; }
        public string CompanyReferenceNumber { get; set; }
        public string CompanyTaxNumber { get; set; }
        public string CompanyEmail { get; set; }
    }
}
