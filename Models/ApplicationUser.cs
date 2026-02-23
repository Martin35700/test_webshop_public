using Microsoft.AspNetCore.Identity;

namespace webshop.Models;

public class ApplicationUser : IdentityUser
{
    // Személyes adatok
    public string FullName { get; set; } = "";

    // Szállítási cím
    public string Zip { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";

    // Számlázási cím (ÚJ)
    public string? BillingZip { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingAddress { get; set; }

    // Egyéb beállítások
    public bool IsNewsletterSubscribed { get; set; } = false;
}