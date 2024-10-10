namespace BumbleBeeWebApp.Models
{
    public class FundingRequest
    {
        public string FundingRequestID { get; set; }
        public string ProjectID { get; set; } 
        public decimal AmountRequested { get; set; }
        public string Status { get; set; }
    }
}
