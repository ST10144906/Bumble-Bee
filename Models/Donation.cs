namespace BumbleBeeWebApp.Models
{
    public class Donation
    {
        public string DonationID { get; set; }
        public string ProjectID { get; set; } 
        public int DonorID { get; set; }  
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
    }
}
