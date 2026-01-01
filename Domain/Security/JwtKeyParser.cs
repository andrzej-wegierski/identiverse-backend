using System.Text;

namespace Domain.Security;

public static class JwtKeyParser
{
    public static byte[] GetSigningKeyBytes(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return Array.Empty<byte>();
        }
        
        if (IsBase64String(signingKey))
        {
            try
            {
                var bytes = Convert.FromBase64String(signingKey);
                if (bytes.Length >= 32)
                {
                    return bytes;
                }
            }
            catch
            {
                // Fallback to UTF8
            }
        }

        return Encoding.UTF8.GetBytes(signingKey);
    }

    private static bool IsBase64String(string s)
    {
        s = s.Trim();
        return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,2}$");
    }
}
