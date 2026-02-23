namespace webshop.Models
{
    public class SiteSettings
    {
        public int Id { get; set; }
        public string Key { get; set; } = ""; // Pl: "FreeShippingLimit"
        public string Value { get; set; } = ""; // Pl: "20000"
    }
}