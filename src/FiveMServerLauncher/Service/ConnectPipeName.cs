using System.Diagnostics;
using System.Security.Principal;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

// Named pipes live in one machine-global namespace ("Local\" means nothing for pipes) while
// the single-instance mutex is per session, so the connect pipe name carries the user SID and
// the session id: two Windows sessions never collide. Listener and forwarder both derive it here.
public static class ConnectPipeName
{
    public static string For(string userSid, int sessionId) =>
        $"{AppInfo.ProductName}.connect.{userSid}.{sessionId}";

    public static string ForCurrentSession()
    {
        using var identity = WindowsIdentity.GetCurrent();
        using var process = Process.GetCurrentProcess();
        return For(identity.User!.Value, process.SessionId);
    }
}
