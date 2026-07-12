namespace Learnix.API.Constants;

/// <summary>
/// SignalR hub routes. <see cref="Prefix"/> is also matched by the JWT bearer handler,
/// which accepts the token from the query string on hub requests only — browsers cannot
/// set an Authorization header on a WebSocket handshake.
/// </summary>
public static class HubRoutes
{
    public const string Prefix = "/hubs";
    public const string Notifications = Prefix + "/notifications";
}
