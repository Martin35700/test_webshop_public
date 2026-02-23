using webshop.Models;

namespace webshop.Services;

using Microsoft.EntityFrameworkCore;
using webshop.Data;
using webshop.Models;

public class ProductService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ProductService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Új aszinkron metódus az adatbázis eléréséhez
    public async Task<List<Product>> GetProductsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Products.ToListAsync();
    }
}