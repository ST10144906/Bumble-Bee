using System.ComponentModel.DataAnnotations;

namespace BumbleBeeWebApp.Models
{
    public class FundingRequest
    {
        [Required]
        public string ProjectId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive number.")]
        public double Amount { get; set; }

        [Required]
        public string Motivation { get; set; }
    }
}
