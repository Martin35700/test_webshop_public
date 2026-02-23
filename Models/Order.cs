using System.ComponentModel.DataAnnotations;

namespace webshop.Models
{

    public enum OrderStatus
    {
        Feldolgozatlan,
        FeldolgozásAlatt,
        Feldolgozva,
        SzállításAlatt,
        Teljesítve,
        Lemondva
    }

    public enum PaymentStatus
    {
        Fizetendő,
        Fizetve,
        Sztornózva
    }
    public class Order
    {
        public int Id { get; set; }

        // Egyedi, véletlenszerű karaktersorozat a publikus rendeléskövetéshez
        public string SecretToken { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "A név megadása kötelező")]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Az e-mail cím kötelező")]
        [EmailAddress(ErrorMessage = "Érvénytelen e-mail formátum")]
        public string Email { get; set; } = "";

        // Opcionális: Ha regisztrált felhasználó adja le
        public string? UserId { get; set; }

        // Szállítási adatok
        [Required(ErrorMessage = "A cím megadása kötelező")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "A város megadása kötelező")]
        public string City { get; set; } = "";

        [Required(ErrorMessage = "Az irányítószám kötelező")]
        public string Zip { get; set; } = "";

        [Required]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string BillingZip { get; set; } = "";
        [Required]
        public string BillingCity { get; set; } = "";
        [Required]
        public string BillingAddress { get; set; } = "";

        public string ShippingMethod { get; set; } = ""; // Pl. Futárszolgálat, Csomagpont
        public string PaymentMethod { get; set; } = "";  // Pl. Bankkártya, Utánvét

        public decimal ShippingFee { get; set; } // Ténylegesen felszámított szállítási díj
        public decimal DiscountAmount { get; set; } // Ténylegesen levont kedvezmény

        // Státuszkezelés
        public OrderStatus Status { get; set; } = OrderStatus.Feldolgozatlan;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Fizetendő;

        // Segédtulajdonságok
        public bool IsPaid => PaymentStatus == PaymentStatus.Fizetve;

        // Ez tárolja a rendelés végösszegét az adatbázisban (fontos a statisztikákhoz)
        public decimal TotalAmount { get; set; }

        // Kapcsolat a tételekkel
        public List<OrderItem> Items { get; set; } = new();

        public int? CouponID { get; set; }

        /// <summary>
        /// Újraszámolja a TotalAmount értékét a tételek alapján.
        /// Ezt érdemes meghívni a rendelés mentése előtt.
        /// </summary>
        public void RecalculateTotal()
        {
            TotalAmount = Items.Sum(i => i.Price * i.Quantity);
        }
    }
}
