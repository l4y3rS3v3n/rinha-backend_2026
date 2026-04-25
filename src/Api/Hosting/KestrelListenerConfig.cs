using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Rinha.Fraud.Hosting;

internal static class KestrelListenerConfig
{
    public static void Configure(KestrelServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddServerHeader = false;
        options.AllowSynchronousIO = false;

        var socket = Environment.GetEnvironmentVariable(ResourcePaths.EnvListenSocket);
        if (!string.IsNullOrEmpty(socket))
        {
            try { File.Delete(socket); } catch (IOException) { }
            options.ListenUnixSocket(socket);
            return;
        }

        var portRaw = Environment.GetEnvironmentVariable(ResourcePaths.EnvListenPort);
        var port = int.TryParse(portRaw, out var parsed) ? parsed : 8080;
        options.ListenAnyIP(port);
    }
}
