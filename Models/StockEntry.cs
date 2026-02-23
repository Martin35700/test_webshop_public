using System.ComponentModel.DataAnnotations;

namespace webshop.Models;

public class StockEntry
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A mennyiségnek legalább 1-nek kell lennie.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "A beszerzési ár nem lehet negatív.")]
    public decimal UnitCost { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public string? Note { get; set; }
}