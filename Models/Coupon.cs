namespace webshop.Models
{
    public enum CouponType
    {
        Percentage,    // -X %
        FixedAmount,   // -X Ft
        FreeShipping   // Ingyenes szállítás
    }

    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = ""; // Pl: NYAR2024
        public CouponType Type { get; set; }
        public decimal Value { get; set; }     // Százalék vagy Összeg
        public decimal MinimumOrderAmount { get; set; } // Az általad említett 'Y' határ

        public int MaxUsages { get; set; }     // 0 = végtelen
        public int UsedCount { get; set; }     // Hányszor használták eddig

        public DateTime? ExpiryDate { get; set; } // Lejárati idő
        public bool IsActive { get; set; } = true;
    }
}
