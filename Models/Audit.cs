namespace BumbleBeeWebApp.Models
{
    public class Audit
    {
        public int AuditID { get; set; }
        public string UID { get; set; }
        public string Email { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
