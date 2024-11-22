using System.Collections.Generic;

namespace BumbleBeeWebApp.Models
{
    public class PaymentViewModel
    {
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string PaymentType { get; set; }
        public string Interval { get; set; }
        public string project { get; set; }
        public List<string> ProjectNames { get; set; }
        public string SelectedProject { get; set; }

    }
}