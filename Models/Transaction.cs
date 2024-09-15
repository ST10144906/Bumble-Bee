namespace BumbleBeeWebApp.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int AuditID { get; set; }
        public int ProjectID { get; set; }
        public int DonorID { get; set; }
        public int DonationID { get; set; }
        public decimal Amount { get; set; }

        public virtual Audit Audit { get; set; }
        public virtual Project Project { get; set; }
        public virtual Donor Donor { get; set; }
        public virtual Donation Donation { get; set; }
    }
}
