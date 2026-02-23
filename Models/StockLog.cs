namespace webshop.Models
{
    public class StockLog
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public int ChangeAmount { get; set; } // Lehet pozitív vagy negatív
        public int ResultStock { get; set; }  // A módosítás utáni pontos érték

        public string Reason { get; set; } = ""; // Megjegyzés
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
