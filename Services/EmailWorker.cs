namespace webshop.Services;

public class EmailWorker : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IServiceProvider _serviceProvider;

    public EmailWorker(EmailQueue queue, IServiceProvider serviceProvider)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _queue.DequeueAsync(stoppingToken);
            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a sorolt email küldésekor: {ex.Message}");
                // Visszatesszük a sor végére, hogy később újra megpróbálja
                _queue.QueueEmail(workItem);
            }
        }
    }
}