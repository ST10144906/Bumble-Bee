using System;
using System.ComponentModel.DataAnnotations;

namespace BumbleBeeWebApp.Models
{
    public class Testimonial
    {
        public string UID { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
