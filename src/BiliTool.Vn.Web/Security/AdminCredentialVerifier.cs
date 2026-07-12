using System.Security.Cryptography;

namespace BiliTool.Vn.Web.Security;

public sealed class AdminCredentialVerifier(IConfiguration configuration)
{
    private const int MinimumIterations = 210_000;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["AdminAuth:Username"]) &&
        !string.IsNullOrWhiteSpace(configuration["AdminAuth:PasswordHash"]);

    public bool Verify(string username, string password)
    {
        var configuredUsername = configuration["AdminAuth:Username"];
        var encodedHash = configuration["AdminAuth:PasswordHash"];
        if (string.IsNullOrWhiteSpace(configuredUsername) || string.IsNullOrWhiteSpace(encodedHash)) return false;

        var usernameMatches = CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(username.PadRight(Math.Max(username.Length, configuredUsername.Length))),
            System.Text.Encoding.UTF8.GetBytes(configuredUsername.PadRight(Math.Max(username.Length, configuredUsername.Length))));

        return usernameMatches && VerifyPassword(password, encodedHash);
    }

    private static bool VerifyPassword(string password, string encodedHash)
    {
        try
        {
            var parts = encodedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations) || iterations < MinimumIterations) return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
