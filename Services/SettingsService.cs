using Microsoft.EntityFrameworkCore;
using webshop.Data;
using webshop.Models;

namespace webshop.Services
{
    public class SettingsService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private Dictionary<string, string> _cache = new();

        public SettingsService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task LoadSettingsAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            _cache = await db.SiteSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        }

        public decimal GetDecimal(string key, decimal defaultValue)
            => _cache.TryGetValue(key, out var val) && decimal.TryParse(val, out var res) ? res : defaultValue;

        public string GetString(string key, string defaultValue)
            => _cache.TryGetValue(key, out var val) ? val : defaultValue;

        public async Task UpdateSettingAsync(string key, string value)
        {
            using var db = _dbFactory.CreateDbContext();
            var setting = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) db.SiteSettings.Add(new SiteSettings { Key = key, Value = value });
            else setting.Value = value;

            await db.SaveChangesAsync();
            await LoadSettingsAsync(); // Frissítjük a gyorsítótárat
        }
    }
}