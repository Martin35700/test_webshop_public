using System.ComponentModel.DataAnnotations;

namespace webshop.Models
{
    public class BlacklistedUser
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        public string? Reason { get; set; } // Pl: "3x nem vette át a csomagot"

        public DateTime AddedAt { get; set; } = DateTime.Now;

        public string? AddedBy { get; set; } // Melyik admin adta hozzá
    }
}