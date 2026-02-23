using webshop.Models;

namespace webshop.Services;

public class CartService
{
    // A kosár tartalma
    public List<CartItem> Items { get; private set; } = new();

    // Esemény, amire a komponensek feliratkozhatnak (StateHasChanged hívásához)
    public event Action? OnChange;

    // 1. Kosár betöltése (pl. LocalStorage-ból vagy DB-ből jövő adat)
    public async Task LoadItemsAsync(List<CartItem> items)
    {
        if (items != null)
        {
            Items = items;
            NotifyStateChanged();
        }
        await Task.CompletedTask; // Mivel nincs valódi I/O művelet, jelezzük a kész állapotot
    }

    // 2. Termék hozzáadása a kosárhoz
    public async Task AddToCartAsync(Product product, int quantity)
    {
        // Megkeressük, van-e már ilyen termék a kosárban
        var item = Items.FirstOrDefault(i => i.Product.Id == product.Id);
        int currentInCart = item?.Quantity ?? 0;
        int newTotalQuantity = currentInCart + quantity;

        // --- ÚJ: MAXIMÁLIS RENDELÉSI LIMIT ELLENŐRZÉSE ---
        if (product.MaxQuantityPerOrder.HasValue && product.MaxQuantityPerOrder > 0)
        {
            if (newTotalQuantity > product.MaxQuantityPerOrder.Value)
            {
                // Kivételt dobunk, amit a hívó oldal (pl. ProductDetails) elkaphat és kiírhat
                throw new InvalidOperationException($"Ebből a termékből maximum {product.MaxQuantityPerOrder} db vásárolható egy rendelésben! A kosaradban már van {currentInCart} db.");
            }
        }

        // --- KÉSZLET ELLENŐRZÉSE (Szigorú mód) ---
        if (product.StrictStockControl)
        {
            if (newTotalQuantity > product.Stock)
            {
                // Ha nincs elég készlet, kivételt dobunk (vagy korrigálhatnánk a mennyiséget)
                throw new InvalidOperationException($"Sajnáljuk, nincs elegendő készlet! Jelenleg elérhető: {product.Stock} db.");
            }
        }

        // Ha a mennyiség érvénytelen, kilépünk
        if (quantity <= 0) return;

        // Hozzáadás vagy frissítés
        if (item != null)
        {
            item.Quantity += quantity;
        }
        else
        {
            Items.Add(new CartItem { Product = product, Quantity = quantity });
        }

        NotifyStateChanged();
        await Task.CompletedTask;
    }

    // 3. Mennyiség frissítése (pl. a Kosár oldalon a +/- gombokkal)
    public async Task UpdateQuantityAsync(int productId, int newQuantity)
    {
        var item = Items.FirstOrDefault(i => i.Product.Id == productId);
        if (item != null)
        {
            // Itt is érdemes lenne ellenőrizni a MaxQuantityPerOrder-t és a Stock-ot,
            // de a UI általában már korlátozza az inputot.
            // A biztonság kedvéért egy alapvető ellenőrzés:

            if (newQuantity > item.Product.Stock && item.Product.StrictStockControl)
            {
                // Ha többet akar, mint ami van, beállítjuk a maximumra
                newQuantity = item.Product.Stock;
            }

            if (item.Product.MaxQuantityPerOrder.HasValue && newQuantity > item.Product.MaxQuantityPerOrder.Value)
            {
                newQuantity = item.Product.MaxQuantityPerOrder.Value;
            }

            item.Quantity = newQuantity;

            if (item.Quantity <= 0)
            {
                await RemoveItemAsync(productId);
            }
            else
            {
                NotifyStateChanged();
            }
        }
        await Task.CompletedTask;
    }

    // 4. Termék törlése a kosárból
    public async Task RemoveItemAsync(int productId)
    {
        var item = Items.FirstOrDefault(i => i.Product.Id == productId);
        if (item != null)
        {
            Items.Remove(item);
            NotifyStateChanged();
        }
        await Task.CompletedTask;
    }

    // 5. Kosár ürítése
    public async Task ClearAsync()
    {
        Items.Clear();
        NotifyStateChanged();
        await Task.CompletedTask;
    }

    // Számított tulajdonság (nem kell async, mert memóriából számol)
    public decimal TotalPrice => Items.Sum(i => i.Product.Price * i.Quantity);

    // Értesítés a komponenseknek
    private void NotifyStateChanged() => OnChange?.Invoke();
}