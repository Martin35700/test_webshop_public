using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using webshop.Models;

namespace webshop.Services;

public class PdfService
{
    public byte[] GenerateOrderPdf(Order order)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // 1. Kiszámoljuk a termékek tiszta részösszegét (kedvezmény nélkül)
        var itemsTotal = order.Items.Sum(i => i.Price * i.Quantity);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                // FEJLÉC
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Modern Webshop").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{DateTime.Now:yyyy.MM.dd.}");
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text($"Rendelésszám: #{order.Id}").FontSize(14).SemiBold();
                        col.Item().Text($"Állapot: {order.Status}");
                    });
                });

                // TARTALOM
                page.Content().PaddingVertical(20).Column(col =>
                {
                    // VEVŐI ADATOK (Szállítás és Számlázás egymás mellett)
                    col.Item().Row(row =>
                    {
                        // Szállítási cím oszlop
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Szállítási cím:").SemiBold().Underline();
                            c.Item().PaddingTop(2).Text(order.CustomerName);
                            c.Item().Text($"{order.Zip} {order.City}");
                            c.Item().Text(order.Address);
                            c.Item().PaddingTop(2).Text($"Tel: {order.PhoneNumber}").Italic();
                            c.Item().Text(order.Email);
                        });

                        // Számlázási cím oszlop
                        row.RelativeItem().PaddingLeft(20).Column(c =>
                        {
                            c.Item().Text("Számlázási cím:").SemiBold().Underline();

                            // Ha van külön számlázási cím megadva, azt írjuk ki, különben a szállításit
                            var bName = order.CustomerName;
                            var bZip = string.IsNullOrEmpty(order.BillingZip) ? order.Zip : order.BillingZip;
                            var bCity = string.IsNullOrEmpty(order.BillingCity) ? order.City : order.BillingCity;
                            var bAddr = string.IsNullOrEmpty(order.BillingAddress) ? order.Address : order.BillingAddress;

                            c.Item().PaddingTop(2).Text(bName);
                            c.Item().Text($"{bZip} {bCity}");
                            c.Item().Text(bAddr);
                        });
                    });

                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Szállítási mód:").SemiBold();
                            c.Item().Text(order.ShippingMethod ?? "Házhozszállítás");
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Fizetési mód:").SemiBold();
                            c.Item().Text(order.PaymentMethod ?? "Utánvét");
                        });
                    });

                    // --- ÁTUTALÁS SPECIFIKUS BLOKK A PDF-BEN ---
                    if (order.PaymentMethod == "Utalás")
                    {
                        col.Item().PaddingTop(15).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Átutalási adatok").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                            c.Item().PaddingTop(5).Row(r =>
                            {
                                r.RelativeItem().Column(labelCol =>
                                {
                                    labelCol.Item().Text("Kedvezményezett:");
                                    labelCol.Item().Text("Számlaszám:");
                                    labelCol.Item().Text("Bank:");
                                    labelCol.Item().Text("Közlemény:");
                                });
                                r.RelativeItem().Column(valueCol =>
                                {
                                    valueCol.Item().Text("Te Cégeted Kft.").SemiBold();
                                    valueCol.Item().Text("HU00 12345678-12345678-12345678").SemiBold();
                                    valueCol.Item().Text("OTP Bank").SemiBold();
                                    valueCol.Item().Text($"{order.Id}").SemiBold().FontColor(Colors.Red.Medium);
                                });
                            });
                            c.Item().PaddingTop(5).Text("Kérjük, az utalás közleményébe kizárólag a rendelésszámot írja be!").FontSize(8).Italic();
                        });
                    }

                    col.Item().PaddingTop(20);

                    // Termék táblázat
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).PaddingVertical(5).Text("Termék").SemiBold();
                            header.Cell().BorderBottom(1).PaddingVertical(5).AlignRight().Text("Egységár").SemiBold();
                            header.Cell().BorderBottom(1).PaddingVertical(5).AlignRight().Text("Mennyiség").SemiBold();
                            header.Cell().BorderBottom(1).PaddingVertical(5).AlignRight().Text("Összesen").SemiBold();
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text(item.ProductName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignRight().Text($"{item.Price:N0} Ft");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignRight().Text($"{item.Quantity} db");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignRight().Text($"{(item.Price * item.Quantity):N0} Ft");
                        }
                    });

                    // ÖSSZESÍTÉS RÉSZ
                    col.Item().AlignRight().PaddingTop(20).Column(totalCol =>
                    {
                        totalCol.Item().Text(x =>
                        {
                            x.Span("Részösszeg: ");
                            x.Span($"{itemsTotal:N0} Ft");
                        });

                        if (order.DiscountAmount > 0)
                        {
                            totalCol.Item().Text(x =>
                            {
                                x.Span("Kedvezmény: ").FontColor(Colors.Red.Medium);
                                x.Span($"-{order.DiscountAmount:N0} Ft").FontColor(Colors.Red.Medium);
                            });
                        }

                        totalCol.Item().Text(x =>
                        {
                            x.Span("Szállítási díj: ");
                            x.Span(order.ShippingFee > 0 ? $"{order.ShippingFee:N0} Ft" : "Ingyenes");
                        });

                        totalCol.Item().PaddingTop(5).Text(x =>
                        {
                            x.Span("Végösszeg: ").FontSize(14).SemiBold();
                            x.Span($"{order.TotalAmount:N0} Ft").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Köszönjük a vásárlást! | Modern Webshop | ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }
}