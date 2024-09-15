namespace BumbleBeeWebApp.Models
{
    public class FundingRequest
    {
        public int FundingRequestID { get; set; }
        public int ProjectID { get; set; }
        public decimal AmountRequested { get; set; }
        public string Status { get; set; }

        public virtual Project Project { get; set; }
    }
}
