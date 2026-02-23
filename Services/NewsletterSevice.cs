using Microsoft.EntityFrameworkCore;
using webshop.Data;
using webshop.Models;

namespace webshop.Services
{
    public class NewsletterService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public NewsletterService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> SubscribeGuestAsync(string email)
        {
            using var context = _dbFactory.CreateDbContext();

            // Ellenőrizzük, hogy létezik-e már
            var existing = await context.NewsletterSubscribers
                .FirstOrDefaultAsync(s => s.Email == email);

            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true; // Visszaaktiváljuk
                    existing.SubscribedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false; // Már fel van iratkozva
            }

            context.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email = email,
                IsActive = true
            });

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnsubscribeAsync(string email)
        {
            using var context = _dbFactory.CreateDbContext();

            // 1. Megnézzük a vendég listát
            var guest = await context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email);
            if (guest != null)
                guest.IsActive = false;

            // 2. Megnézzük a regisztrált felhasználókat
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
                user.IsNewsletterSubscribed = false;

            await context.SaveChangesAsync();
            return true;
        }

        // Ez a metódus visszaadja az összes EGYEDI email címet, akinek küldhetünk
        public async Task<List<string>> GetAllSubscribersAsync()
        {
            using var context = _dbFactory.CreateDbContext();

            // Regisztráltak, akik kérték
            var userEmails = await context.Users
                .Where(u => u.IsNewsletterSubscribed && u.Email != null)
                .Select(u => u.Email!)
                .ToListAsync();

            // Vendégek
            var guestEmails = await context.NewsletterSubscribers
                .Where(s => s.IsActive)
                .Select(s => s.Email)
                .ToListAsync();

            // Unió (hogy ne kapja meg kétszer, ha mindkét helyen szerepel)
            return userEmails.Union(guestEmails).Distinct().ToList();
        }
    }
}