using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Auth;

public static class PasswordHelper
{
    private static readonly PasswordHasher<object> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(new object(), password);

    public static bool Verify(string storedHash, string password)
    {
        try
        {
            return Hasher.VerifyHashedPassword(new object(), storedHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}