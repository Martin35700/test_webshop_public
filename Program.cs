using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webshop.Components;
using webshop.Data;
using webshop.Models;
using webshop.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ALAPSZOLGÁLTATÁSOK ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddAntiforgery();

// --- 2. SAJÁT SZERVIZEK ---
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<NewsletterService>();

// Singleton és Hosted Service-ek (Háttérfeladatok)
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailWorker>();
builder.Services.AddHostedService<OrderCleanupService>();
builder.Services.AddHostedService<InventoryMonitorService>();

// --- 3. ADATBÁZIS ÉS BIZTONSÁG ---
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Azonnali kiléptetés tiltás esetén (minden kérésnél ellenõrzi az adatbázist)
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=webshop.db"));

// --- 4. IDENTITY KONFIGURÁCIÓ ---
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
{
    // Jelszó házirend (igény szerint módosítható)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

// Blazor specifikus Identity State Provider
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

// --- 5. HTTP REQUEST PIPELINE (Middleware sorrend fontos!) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.MapStaticAssets();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();


// --- 6. ADATBÁZIS SEEDING (ADMIN LÉTREHOZÁSA) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var contextFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = contextFactory.CreateDbContext();

        // Adatbázis létrehozása / Migrációk futtatása ---
        // Ha nincs fájl, létrehozza a legfrissebb Migration-ök alapján. 
        // Ha van, de hiányzik egy tábla, hozzáadja.
        if (context.Database.GetPendingMigrations().Any() || !context.Database.CanConnect())
        {
            logger.LogInformation("Adatbázis inicializálása és migrációk futtatása...");
            await context.Database.MigrateAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Szerepkör biztosítása
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // 2. Admin felhasználó keresése
        var adminEmail = "admin@webshop.hu";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Rendszergazda",
                Zip = "1000",
                City = "Budapest",
                Address = "Központ"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Default admin sikeresen létrehozva.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Hiba történt az adatbázis inicializálása közben.");
    }
}


// --- 7. VÉGPONTOK (ENDPOINTS) ---

app.MapGroup("/Account").MapIdentityApi<ApplicationUser>();

app.MapControllers();

// Kijelentkezés (POST) - Biztonságosabb
app.MapPost("/Account/Logout", async (
    SignInManager<ApplicationUser> signInManager,
    [FromForm] string returnUrl) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect($"/{returnUrl ?? ""}");
}).DisableAntiforgery(); 

// Kijelentkezés (GET) - Kényelmi funkció linkekhez
app.MapGet("/Account/LogoutAction", async (
    SignInManager<ApplicationUser> signInManager,
    string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect(returnUrl ?? "/");
});

// Blazor komponensek
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();