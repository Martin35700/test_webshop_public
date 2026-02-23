using Microsoft.EntityFrameworkCore;
using webshop.Data;
using webshop.Models;

namespace webshop.Services;

public class OrderCleanupService : BackgroundService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<OrderCleanupService> _logger;
    // Mennyi idő után számítson elévültnek a kifizetetlen rendelés (pl. 60 perc)
    private readonly int _timeoutMinutes = 60;

    public OrderCleanupService(IDbContextFactory<AppDbContext> dbFactory, ILogger<OrderCleanupService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rendelés takarító szolgálat elindult.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOrders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba történt a kifizetetlen rendelések takarítása közben.");
            }

            // Várjunk 30 percet a következő futtatásig
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task CleanupOrders()
    {
        using var context = _dbFactory.CreateDbContext();
        var cutoffTime = DateTime.Now.AddMinutes(-_timeoutMinutes);

        // Olyan kártyás rendelések, amik még 'Fizetendő' státuszúak és elévültek
        var expiredOrders = await context.Orders
            .Include(o => o.Items)
            .Where(o => o.PaymentMethod == "Bankkártya" &&
                        o.PaymentStatus == PaymentStatus.Fizetendő &&
                        o.OrderDate < cutoffTime)
            .ToListAsync();

        if (expiredOrders.Any())
        {
            _logger.LogInformation($"{expiredOrders.Count} db elévült rendelés feldolgozása...");

            foreach (var order in expiredOrders)
            {
                // KÉSZLET VISSZATÖLTÉSE
                foreach (var item in order.Items)
                {
                    var product = await context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;

                        context.StockLogs.Add(new StockLog
                        {
                            ProductId = product.Id,
                            ChangeAmount = item.Quantity,
                            ResultStock = product.Stock,
                            Reason = $"Automata törlés (lejárt kártyás fizetés #{order.Id})",
                            Date = DateTime.Now
                        });
                    }
                }

                order.Status = OrderStatus.Lemondva;
                order.PaymentStatus = PaymentStatus.Sztornózva;

                context.Orders.Update(order);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Takarítás sikeresen befejeződött.");
        }
    }
}