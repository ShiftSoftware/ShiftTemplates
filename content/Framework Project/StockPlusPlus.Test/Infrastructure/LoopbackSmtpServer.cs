using System.Net;
using System.Net.Sockets;
using System.Text;
using MimeKit;

namespace StockPlusPlus.Test.Infrastructure;

/// <summary>One fixture-owned loopback connection. Never authenticates, relays, logs or writes messages to disk.</summary>
internal sealed class LoopbackSmtpServer : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stop = new();
    private readonly Task serving;
    public TaskCompletionSource<MimeMessage> Message { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Acceptance { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string? EnvelopeRecipient { get; private set; }
    public int Port { get; }

    public LoopbackSmtpServer()
    {
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        serving = ServeAsync();
    }

    private async Task ServeAsync()
    {
        try
        {
            using var socket = await listener.AcceptTcpClientAsync(stop.Token);
            await using var stream = socket.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
            await writer.WriteLineAsync("220 localhost fixture SMTP");
            while (await reader.ReadLineAsync(stop.Token) is { } command)
            {
                if (command.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase))
                    await writer.WriteLineAsync("250 localhost");
                else if (command.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
                    await writer.WriteLineAsync("250 Sender accepted");
                else if (command.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                {
                    EnvelopeRecipient = command[8..].Trim();
                    await writer.WriteLineAsync("250 Recipient accepted");
                }
                else if (command == "DATA")
                {
                    await writer.WriteLineAsync("354 Send content");
                    var content = new StringBuilder();
                    while (await reader.ReadLineAsync(stop.Token) is { } line && line != ".")
                        content.Append(line.StartsWith("..", StringComparison.Ordinal) ? line[1..] : line).Append("\r\n");
                    using var data = new MemoryStream(Encoding.UTF8.GetBytes(content.ToString()));
                    Message.TrySetResult(await MimeMessage.LoadAsync(data, stop.Token));
                    var accepted = await Acceptance.Task.WaitAsync(stop.Token);
                    await writer.WriteLineAsync(accepted ? "250 Accepted by fixture" : "550 Synthetic refusal");
                }
                else if (command == "QUIT") { await writer.WriteLineAsync("221 Goodbye"); break; }
                else if (command == "RSET") await writer.WriteLineAsync("250 Reset");
                else await writer.WriteLineAsync("502 Unsupported by fixture");
            }
        }
        catch (Exception error) when (stop.IsCancellationRequested && error is OperationCanceledException or IOException or SocketException) { }
        catch (Exception error) { Message.TrySetException(error); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        listener.Stop();
        await serving;
        if (Message.Task.IsCompletedSuccessfully) Message.Task.Result.Dispose();
        stop.Dispose();
    }
}
