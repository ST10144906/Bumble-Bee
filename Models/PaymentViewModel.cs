namespace BumbleBeeWebApp.Models
{
    public class PaymentViewModel
    {
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; }
        public string PublicKey { get; set; } // Your Paystack public key
    }
}
