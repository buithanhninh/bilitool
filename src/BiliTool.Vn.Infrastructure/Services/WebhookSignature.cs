using System.Security.Cryptography;
using System.Text;

namespace BiliTool.Vn.Infrastructure.Services;

public static class WebhookSignature
{
    public static string Create(string secret, long unixTimestamp, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{unixTimestamp}.{payload}"));
        return $"v1={Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
