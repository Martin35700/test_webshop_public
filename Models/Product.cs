namespace webshop.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = "https://via.placeholder.com/300";
        public int QuantityToBuy { get; set; } = 1; // A UI-hoz kell a darabszám választáshoz

        public int LowStockThreshold { get; set; } = 5; // Alapértelmezett kritikus szint
        public bool IsLowStockAlertSent { get; set; } = false; // Jeleztük-e már a problémát?

        // Ha null vagy 0, akkor nincs korlátozás.
        // Ha pl. 5, akkor egy rendelésben max 5 db lehet ebből.
        public int? MaxQuantityPerOrder { get; set; }

        // Aktuális darabszám a raktárban
        public int Stock { get; set; } = 0;

        // Ha ez true, a termék CSAK akkor rendelhető, ha Stock > 0
        // Ha false, akkor is rendelhető, ha elfogyott (pl. utánrendelés alatt)
        public bool StrictStockControl { get; set; } = false;

        // Ha ez false, a termék nem jelenik meg a webshopban (pl. kivezetett termék)
        public bool IsActive { get; set; } = true;

        // ÚJ: Kapcsolat a kategóriával
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public List<ProductReview> Reviews { get; set; } = new();

        public List<ProductImage> GalleryImages { get; set; } = new();
    }
}
