namespace webshop.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // Aki írta
        public string? UserId { get; set; }
        public string UserName { get; set; } = "Vendég"; // Ha nem regisztrált, vagy törölte magát

        // Az értékelés tartalma
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Adminisztrációs célra
        public bool IsApproved { get; set; } = false;
    }
}
