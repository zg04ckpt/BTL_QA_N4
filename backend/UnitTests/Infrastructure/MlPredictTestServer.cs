using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace UnitTests.Infrastructure;

/// <summary>
/// HTTP listener trả JSON ML <c>predicted_class_id</c> cho ReviewService (POST body có field comment).
/// </summary>
public sealed class MlPredictTestServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public string PredictUrl { get; }

    /// <summary>Lớp dự đoán (0 = cho phép).</summary>
    public int PredictedClassId { get; set; }

    /// <summary>Khi true: trả HTTP 503, không JSON.</summary>
    public bool ReturnServiceUnavailable { get; set; }

    public MlPredictTestServer()
    {
        var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();

        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        PredictUrl = $"http://127.0.0.1:{port}/predict";

        var token = _cts.Token;
        _loop = Task.Run(() => ListenLoopAsync(token), token);
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(ctx), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            if (ReturnServiceUnavailable)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                ctx.Response.Close();
                return;
            }

            var payload = JsonSerializer.Serialize(new { predicted_class_id = PredictedClassId });
            var buf = Encoding.UTF8.GetBytes(payload);
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.OutputStream.WriteAsync(buf);
            ctx.Response.Close();
        }
        catch
        {
            try { ctx.Response.Abort(); } catch { /* ignore */ }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
        try { _listener.Close(); } catch { /* ignore */ }
        try { _loop?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _cts.Dispose();
    }
}
