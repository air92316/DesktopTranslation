using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace DesktopTranslation.Helpers;

public static class DataProtectionHelper
{
    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return "";

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(
                plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (CryptographicException ex)
        {
            Debug.WriteLine($"DPAPI Protect failed: {ex.Message}");
            return "";
        }
    }

    public static string Unprotect(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64))
            return "";

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            Debug.WriteLine($"DPAPI Unprotect failed: {ex.Message}");
            return "";
        }
        catch (FormatException ex)
        {
            Debug.WriteLine($"DPAPI Unprotect base64 decode failed: {ex.Message}");
            return "";
        }
    }
}
