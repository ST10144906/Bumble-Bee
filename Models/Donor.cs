namespace BumbleBeeWebApp.Models
{
    public class Donor
    {
        public int DonorID { get; set; }
        public string UID { get; set; }
        public string DonorName { get; set; }
        public string SouthAfricaId { get; set; }
        public string TaxNumber { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public virtual ICollection<Donation> Donations { get; set; }
    }
}
