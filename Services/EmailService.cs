using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using webshop.Models;

namespace webshop.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // Segédmetódus az alap URL lekéréséhez a konfigurációból
    private string GetBaseUrl()
    {
        var url = _config["AppUrl"];

        if (string.IsNullOrEmpty(url))
        {
            return "https://localhost:7188/";
        }

        return url.EndsWith("/") ? url : url + "/";
    }

    private async Task<bool> SendEmailInternalAsync(string toEmail, string toName, string subject, string htmlBody, string? attachmentPath = null)
    {
        var message = new MimeMessage();
        var smtpUser = _config["Email:SmtpUser"];

        message.From.Add(new MailboxAddress("Modern Webshop", smtpUser));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

        if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
        {
            bodyBuilder.Attachments.Add(attachmentPath);
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _config["Email:SmtpServer"],
                int.Parse(_config["Email:SmtpPort"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(_config["Email:SmtpUser"], _config["Email:SmtpPass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email küldési hiba: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        // Mivel a hírleveleknél gyakran nincs név (csak email cím),
        // a nevet ideiglenesen az email címmel helyettesítjük.
        return await SendEmailInternalAsync(toEmail, toEmail, subject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; border-radius: 10px; padding: 20px;'>
                <h2 style='color: #333;'>Jelszó visszaállítása</h2>
                <p>Kérted a jelszavad visszaállítását a Modern Webshop fiókodhoz. Kattints az alábbi gombra az új jelszó megadásához:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Új jelszó beállítása</a>
                </div>
                <p style='color: #666; font-size: 14px;'>Ha nem te kérted a visszaállítást, nyugodtan hagyd figyelmen kívül ezt a levelet.</p>
                <hr style='border: 0; border-top: 1px solid #eee;' />
                <p style='font-size: 12px; color: #888; text-align: center;'>Modern Webshop csapata</p>
            </div>";

        await SendEmailInternalAsync(toEmail, toEmail, "Jelszó visszaállítási kérelem", body);
    }

    public async Task SendOrderConfirmationEmailAsync(Order order, string pdfPath)
    {
        var trackingUrl = $"{GetBaseUrl()}track-order/{order.SecretToken}";

        string bankTransferBlock = "";

        if (order.PaymentMethod == "Utalás")
        {
            bankTransferBlock = $@"
            <div style='background-color: #f8f9fa; border: 2px solid #007bff; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                <h3 style='color: #007bff; margin-top: 0;'>Banki átutalási adatok</h3>
                <p style='margin-bottom: 15px;'>Kérjük, a rendelés végösszegét az alábbi adatokkal utald el:</p>
                <table style='width: 100%; font-size: 14px; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Kedvezményezett:</td>
                        <td style='padding: 5px 0; font-weight: bold;'>Te Cégeted Kft.</td>
                    </tr>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Számlaszám:</td>
                        <td style='padding: 5px 0; font-weight: bold;'>HU00 12345678-12345678-12345678</td>
                    </tr>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Bank:</td>
                        <td style='padding: 5px 0; font-weight: bold;'>OTP Bank</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; color: #666;'>Összeg:</td>
                        <td style='padding: 10px 0; font-weight: bold; color: #007bff; font-size: 18px;'>{order.TotalAmount:N0} Ft</td>
                    </tr>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Közlemény:</td>
                        <td style='padding: 5px 0; font-weight: bold; color: #d9534f;'>{order.Id}</td>
                    </tr>
                </table>
                <p style='font-size: 12px; color: #d9534f; margin-top: 15px; font-style: italic;'>
                    Fontos: A közleménybe kérjük, CSAK a rendelésszámot írd be!
                </p>
            </div>";
        }

        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; line-height: 1.5;'>
            <h2>Köszönjük a rendelést!</h2>
            <p>Kedves {order.CustomerName}, rendelésedet rögzítettük.</p>
            
            {bankTransferBlock}

            <p>A rendelésed állapotát bármikor nyomon követheted az alábbi gombra kattintva:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{trackingUrl}' style='background-color: #007bff; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Rendelés követése</a>
            </div>
            
            <div style='background-color: #eee; padding: 15px; border-radius: 5px;'>
                <p style='margin: 0;'><strong>Rendelésszám:</strong> #{order.Id}</p>
                <p style='margin: 5px 0 0 0;'><strong>Végösszeg:</strong> {order.TotalAmount:N0} Ft</p>
                <p style='margin: 5px 0 0 0;'><strong>Fizetési mód:</strong> {order.PaymentMethod}</p>
            </div>

            <p style='margin-top: 25px;'>Üdvözlettel,<br/>A Modern Webshop csapata</p>
        </div>";

        await SendEmailInternalAsync(order.Email, order.CustomerName, $"Rendelés visszaigazolás - #{order.Id}", body, pdfPath);
    }

    public async Task SendStatusUpdateEmailAsync(Order order, string subject)
    {
        var trackingUrl = $"{GetBaseUrl()}track-order/{order.SecretToken}";

        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 10px; padding: 20px;'>
            <h2 style='color: #0d6efd;'>Jó hírünk van, {order.CustomerName}!</h2>
            <p style='font-size: 16px; color: #333;'>
                Örömmel értesítünk, hogy a <strong>#{order.Id}</strong> azonosítójú rendelésedet munkatársaink 
                <strong>összekészítették és becsomagolták</strong>.
            </p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{trackingUrl}' style='background-color: #0d6efd; color: white; padding: 12px 25px; text-decoration: none; border-radius: 50px; font-weight: bold;'>Rendelés nyomon követése</a>
            </p>
        </div>";

        await SendEmailInternalAsync(order.Email, order.CustomerName, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string userEmail, string userName)
    {
        var appUrl = GetBaseUrl();

        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 10px; overflow: hidden;'>
                <div style='background-color: #007bff; padding: 20px; text-align: center; color: white;'>
                    <h1 style='margin:0;'>Üdvözlünk nálunk!</h1>
                </div>
                <div style='padding: 30px; line-height: 1.6; color: #333;'>
                    <p>Kedves <strong>{userName}</strong>!</p>
                    <p>Örömmel értesítünk, hogy a regisztrációd sikeresen megtörtént.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{appUrl}' style='background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Irány a webshop</a>
                    </div>
                </div>
            </div>";

        await SendEmailInternalAsync(userEmail, userName, "Üdvözöljük a Modern Webshopban!", body);
    }

    public async Task SendContactEmailAsync(string visitorName, string visitorEmail, string subject, string messageText)
    {
        var adminEmail = _config["Email:SmtpUser"];

        var body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                <h2 style='color: #007bff;'>Új üzenet érkezett</h2>
                <p><strong>Feladó:</strong> {visitorName} ({visitorEmail})</p>
                <hr>
                <p><strong>Üzenet:</strong></p>
                <p style='white-space: pre-wrap;'>{messageText}</p>
            </div>";

        await SendEmailInternalAsync(adminEmail!, "Adminisztrátor", $"Kapcsolatfelvétel: {subject}", body);
    }

    public async Task<bool> SendLowStockAlertEmailAsync(List<Product> products)
    {
        var adminEmail = _config["Email:SmtpUser"];

        if (string.IsNullOrEmpty(adminEmail))
        {
            return false;
        }

        var sb = new StringBuilder();

        sb.AppendLine("<h2 style='color: #d9534f;'>Kritikus készletszint értesítés</h2>");
        sb.AppendLine("<p>Az alábbi termékek készlete a beállított küszöbérték alá süllyedt:</p>");
        sb.AppendLine("<table border='1' cellpadding='10' style='border-collapse: collapse; width: 100%; font-family: sans-serif;'>");
        sb.AppendLine("<thead style='background-color: #f8f9fa;'><tr><th>ID</th><th>Termék neve</th><th>Jelenlegi készlet</th><th>Küszöbérték</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var p in products)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>#{p.Id}</td>");
            sb.AppendLine($"<td><strong>{p.Name}</strong></td>");
            sb.AppendLine($"<td style='color: red; font-weight: bold;'>{p.Stock} db</td>");
            sb.AppendLine($"<td>{p.LowStockThreshold} db</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("<p style='margin-top: 20px;'>Kérjük, intézkedjen a készletek feltöltéséről az adminisztrációs felületen.</p>");

        return await SendEmailAsync(adminEmail, "⚠️ KRITIKUS KÉSZLETSZINT - Webshop", sb.ToString());
    }

    public async Task SendOrderCompletedReviewEmailAsync(Order order)
    {
        var appUrl = GetBaseUrl();
        var sb = new StringBuilder();

        sb.AppendLine($@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; line-height: 1.6; color: #333;'>
            <div style='background-color: #198754; padding: 20px; text-align: center; color: white; border-radius: 10px 10px 0 0;'>
                <h1 style='margin:0;'>Rendelés Teljesítve!</h1>
            </div>
            <div style='padding: 20px; border: 1px solid #eee; border-top: none; border-radius: 0 0 10px 10px;'>
                <p>Kedves <strong>{order.CustomerName}</strong>!</p>
                <p>Örömmel értesítünk, hogy a <strong>#{order.Id}</strong> azonosítójú rendelésedet sikeresen teljesítettük. Reméljük, elégedett vagy a vásárolt termékekkel!</p>
                
                <h3 style='color: #198754; margin-top: 30px;'>Véleményed számít nekünk!</h3>
                <p>Kérjük, szánj egy percet a megvásárolt termékek értékelésére, ezzel segítve más vásárlók döntését és a mi munkánkat is:</p>
                
                <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>");

        foreach (var item in order.Items)
        {
            // SEO barát slug generálás a linkhez (ha szükséges)
            var slug = Regex.Replace(item.ProductName.ToLower(), @"[^a-z0-9\s-]", "").Replace(" ", "-");
            var reviewUrl = $"{appUrl}product/{item.ProductId}/{slug}";

            sb.AppendLine($@"
            <tr>
                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>
                    <a href='{reviewUrl}' style='background-color: #0d6efd; color: white; padding: 8px 15px; text-decoration: none; border-radius: 5px; font-size: 13px; font-weight: bold; display: inline-block;'>ÉRTÉKELÉS</a>
                </td>
            </tr>");
        }

        sb.AppendLine($@"
                </table>

                <p style='margin-top: 30px;'>Még egyszer köszönjük, hogy minket választottál!</p>
                <hr style='border: 0; border-top: 1px solid #eee;' />
                <p style='font-size: 12px; color: #888; text-align: center;'>Modern Webshop csapata</p>
            </div>
        </div>");

        await SendEmailInternalAsync(order.Email, order.CustomerName, $"Köszönjük a vásárlást! Értékeld a termékeket (#{order.Id})", sb.ToString());
    }
}