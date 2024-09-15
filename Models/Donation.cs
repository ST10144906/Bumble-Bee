namespace BumbleBeeWebApp.Models
{
    public class Donation
    {
        public int DonationID { get; set; }
        public int ProjectID { get; set; }
        public int DonorID { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }

        public virtual Project Project { get; set; }
        public virtual Donor Donor { get; set; }
    }
}
