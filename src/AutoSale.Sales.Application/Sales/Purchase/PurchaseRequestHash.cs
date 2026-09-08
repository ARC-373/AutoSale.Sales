using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AutoSale.Domain.Buyers;

namespace AutoSale.Application.Sales.Purchase;

public static class PurchaseRequestHash
{
    public static string Create(Guid vehicleId, BuyerCpf buyerCpf, decimal expectedPrice)
    {
        var canonicalPayload = string.Create(CultureInfo.InvariantCulture,
            $"{vehicleId:N}|{buyerCpf.Value}|{expectedPrice:0.00}");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)));
    }
}
