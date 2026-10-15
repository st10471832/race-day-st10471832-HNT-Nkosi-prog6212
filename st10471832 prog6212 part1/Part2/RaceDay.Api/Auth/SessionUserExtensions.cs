using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Auth;

public static class SessionUserExtensions
{
    public static int? GetUserId(this ISession session) => session.GetInt32(SessionKeys.UserId);

    public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static void SignIn(this ISession session, int userId, string role)
    {
        session.Clear();
        session.SetInt32(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.Role, role);
    }
}