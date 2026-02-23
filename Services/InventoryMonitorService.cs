using Microsoft.EntityFrameworkCore;
using webshop.Data;
using webshop.Models;

namespace webshop.Services;

public class InventoryMonitorService : BackgroundService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
    //private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public InventoryMonitorService(
        IDbContextFactory<AppDbContext> dbFactory,
        IServiceProvider serviceProvider)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Várjunk egy kicsit az indítás után, hogy a rendszer felálljon
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckInventoryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba az InventoryMonitorService futása közben: {ex.Message}");
            }

            // Várakozás a következő ellenőrzésig
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckInventoryAsync()
    {
        using var context = _dbFactory.CreateDbContext();

        // 1. Kikeressük azokat a termékeket, amik kritikus szinten vannak és MÉG NEM küldtünk róluk értesítést
        var criticalProducts = await context.Products
            .Where(p => p.IsActive && p.Stock <= p.LowStockThreshold && !p.IsLowStockAlertSent)
            .ToListAsync();

        if (criticalProducts.Any())
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            // Értesítés küldése az adminnak
            bool emailSent = await emailService.SendLowStockAlertEmailAsync(criticalProducts);

            if (emailSent)
            {
                // Ha sikeres az email, bejelöljük a termékeket, hogy ne küldjük újra
                foreach (var product in criticalProducts)
                {
                    product.IsLowStockAlertSent = true;
                }

                await context.SaveChangesAsync();
            }
        }

        // 2. RESET LOGIKA: Ha egy termék készletét feltöltötték a küszöb fölé, töröljük a jelzőt
        var restockedProducts = await context.Products
            .Where(p => p.Stock > p.LowStockThreshold && p.IsLowStockAlertSent)
            .ToListAsync();

        if (restockedProducts.Any())
        {
            foreach (var product in restockedProducts)
            {
                product.IsLowStockAlertSent = false;
            }

            await context.SaveChangesAsync();
        }
    }
}