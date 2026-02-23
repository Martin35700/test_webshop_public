using System.ComponentModel.DataAnnotations;

namespace webshop.Models
{
    public class NewsletterSubscriber
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime SubscribedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true; // Ha leiratkozik, false-ra állítjuk
    }
}