namespace BumbleBeeWebApp.Models
{
    public class AuditLog
    {
        public string AuditLogId { get; set; }
        public string DonationId { get; set; }
        public string ProjectName { get; set; }
        public string UserId { get; set; }
        public double Amount { get; set; }
        public string PaymentType { get; set; }
        public DateTime AuditedAt { get; set; }
        public string Notes { get; set; }
    }
}
