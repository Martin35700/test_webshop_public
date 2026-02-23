using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using webshop.Data;

namespace webshop.Controllers;

[Route("sitemap.xml")]
public class SitemapController : Controller
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SitemapController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetSitemap()
    {
        using var context = _dbFactory.CreateDbContext();

        // Csak az aktív termékeket kérjük le
        var products = await context.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name }) // Csak a szükséges mezőket kérjük le a memóriába
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // --- FŐOLDAL ---
        sb.AppendLine("<url>");
        sb.AppendLine($"<loc>{baseUrl}/</loc>");
        sb.AppendLine("<changefreq>daily</changefreq>");
        sb.AppendLine("<priority>1.0</priority>");
        sb.AppendLine("</url>");

        // --- SHOP OLDAL ---
        sb.AppendLine("<url>");
        sb.AppendLine($"<loc>{baseUrl}/shop</loc>");
        sb.AppendLine("<changefreq>daily</changefreq>");
        sb.AppendLine("<priority>0.8</priority>");
        sb.AppendLine("</url>");

        // --- TERMÉKEK ---
        foreach (var p in products)
        {
            // Legeneráljuk a SEO-barát Slug-ot a név alapján
            string slug = GenerateSlug(p.Name);

            sb.AppendLine("<url>");
            // FONTOS: Most már az ID és a Slug is bekerül az URL-be
            sb.AppendLine($"<loc>{baseUrl}/product/{p.Id}/{slug}</loc>");
            sb.AppendLine($"<lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("<changefreq>weekly</changefreq>");
            sb.AppendLine("<priority>0.6</priority>");
            sb.AppendLine("</url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// URL-barát név generálása (ugyanaz a logika, mint a komponensekben)
    /// </summary>
    private string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "product";
        }

        string slug = name.ToLower();

        // Ékezetek eltávolítása
        slug = Regex.Replace(slug, @"[áàäâ]", "a");
        slug = Regex.Replace(slug, @"[éèëê]", "e");
        slug = Regex.Replace(slug, @"[íìïî]", "i");
        slug = Regex.Replace(slug, @"[óòöőô]", "o");
        slug = Regex.Replace(slug, @"[úùüűû]", "u");

        // Minden egyéb nem betű/szám karakter eltávolítása (kivéve szóköz és kötőjel)
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        // Szóközök és ismétlődő kötőjelek cseréje egyetlen kötőjelre
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");

        return slug.Trim('-');
    }
}