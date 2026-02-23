namespace webshop.Models
{
    public class CouponUsage
    {
        public int Id { get; set; }
        public int CouponId { get; set; }
        public string? UserId { get; set; } = "";

        public string Email { get; set; } = string.Empty;
        public DateTime UsedAt { get; set; } = DateTime.Now;
    }
}
