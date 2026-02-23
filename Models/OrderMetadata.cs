using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace webshop.Models;

public class OrderMetadata : IValidatableObject
{
    [Required(ErrorMessage = "A név megadása kötelező a kiszállításhoz.")]
    [RegularExpression(@"^[a-zA-ZáéíóöőúüűÁÉÍÓÖŐÚÜŰ\s\-]+$", ErrorMessage = "A név csak betűket tartalmazhat.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Az e-mail cím megadása kötelező")]
    [EmailAddress(ErrorMessage = "Érvénytelen e-mail formátum")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "A telefonszám megadása kötelező.")]
    [RegularExpression(@"^(\+?[0-9\s]{7,18})$", ErrorMessage = "Kérjük, érvényes formátumú telefonszámot adjon meg!")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "A pontos cím kötelező.")]
    [MinLength(5, ErrorMessage = "Kérjük, adjon meg pontosabb címet (utca, házszám).")]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "A város megadása kötelező.")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Az irányítószám kötelező.")]
    [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "A magyar irányítószám 4 számjegyből áll.")]
    public string Zip { get; set; } = "";

    // --- Számlázási adatok ---
    public bool BillingSameAsShipping { get; set; } = true;
    public string BillingZip { get; set; } = "";
    public string BillingCity { get; set; } = "";
    public string BillingAddress { get; set; } = "";

    // --- Rendelési opciók ---
    public string ShippingMethod { get; set; } = "Házhozszállítás";
    public string PaymentMethod { get; set; } = "Bankkártya";
    public bool CreateAccount { get; set; }
    public string? Password { get; set; } = "";

    /// <summary>
    /// Egyedi validációs logika a számlázási címhez
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Ha a számlázási cím eltér a szállításitól, ellenőrizzük a mezőket
        if (!BillingSameAsShipping)
        {
            if (string.IsNullOrWhiteSpace(BillingZip) || !Regex.IsMatch(BillingZip, @"^[0-9]{4}$"))
            {
                yield return new ValidationResult("A számlázási irányítószám (4 számjegy) megadása kötelező.", new[] { nameof(BillingZip) });
            }

            if (string.IsNullOrWhiteSpace(BillingCity))
            {
                yield return new ValidationResult("A számlázási város megadása kötelező.", new[] { nameof(BillingCity) });
            }

            if (string.IsNullOrWhiteSpace(BillingAddress) || BillingAddress.Length < 5)
            {
                yield return new ValidationResult("A pontos számlázási cím megadása kötelező.", new[] { nameof(BillingAddress) });
            }
        }

        // Ha regisztrálni is akar, kötelező a jelszó
        if (CreateAccount && (string.IsNullOrWhiteSpace(Password) || Password.Length < 6))
        {
            yield return new ValidationResult("A fiók létrehozásához legalább 6 karakteres jelszó szükséges.", new[] { nameof(Password) });
        }
    }
}