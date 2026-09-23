using System.Security.Cryptography;
using System.Text;

namespace Commerce.Application.Features.DataIngestion;

public static class DataIngestContentHash
{
    public static string Compute(string? payloadJson)
    {
        var bytes = Encoding.UTF8.GetBytes(payloadJson ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
