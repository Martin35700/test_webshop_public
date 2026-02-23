using Stripe;
using Stripe.Checkout;
using webshop.Models;

namespace webshop.Services;

public class StripeService
{
    private readonly IConfiguration _config;

    public StripeService(IConfiguration config)
    {
        _config = config;
        // Beállítjuk a titkos kulcsot a konfigurációból
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<bool> IsPaymentSuccessful(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);
        return session.PaymentStatus == "paid";
    }
    public async Task<string> CreateCheckoutSessionAsync(Order order, string baseUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
            SuccessUrl = $"{baseUrl}order-success/{order.SecretToken}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}cart?canceled_order_token={order.SecretToken}",
            CustomerEmail = order.Email,
        };

        // 1. Termékek hozzáadása (eredeti áron)
        foreach (var item in order.Items)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)item.Price * 100, // Ft -> fillér
                    Currency = "huf",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName,
                    },
                },
                Quantity = item.Quantity,
            });
        }

        // 2. KEDVEZMÉNY HOZZÁADÁSA (Ha van)
        // A Stripe-nál nem lehet negatív összeget megadni a UnitAmount-ban, 
        // ezért "Discounts" listát vagy kupon rendszert kellene használni. 
        // A legegyszerűbb megoldás itt a "Coupon" API használata helyett 
        // egy manuális negatív tétel, de a Stripe ezt csak bizonyos esetekben engedi.

        // Javasolt megoldás: Ha van kedvezmény, adjunk hozzá egy manuális kedvezményt:
        if (order.DiscountAmount > 0)
        {
            options.Discounts = new List<SessionDiscountOptions>
        {
            new SessionDiscountOptions
            {
                Coupon = await CreateTemporaryStripeCoupon(order.DiscountAmount)
            }
        };
        }

        // 3. Szállítási díj hozzáadása
        if (order.ShippingFee > 0)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)order.ShippingFee * 100,
                    Currency = "huf",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Szállítási díj",
                    },
                },
                Quantity = 1,
            });
        }

        var service = new SessionService();
        Session session = await service.CreateAsync(options);
        return session.Url;
    }

    private async Task<string> CreateTemporaryStripeCoupon(decimal discountAmountInHuf)
    {
        var options = new CouponCreateOptions
        {
            AmountOff = (long)discountAmountInHuf * 100, // Fillérben
            Currency = "huf",
            Duration = "once",
            Name = "Felhasznált kupon kedvezmény"
        };

        var service = new CouponService();
        var stripeCoupon = await service.CreateAsync(options);
        return stripeCoupon.Id;
    }
}