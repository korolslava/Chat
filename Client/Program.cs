using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 5000;

var client = new TcpClient();
client.Connect(IPAddress.Loopback, port);

var stream = client.GetStream();
var cts = new CancellationTokenSource();

var readerTask = Task.Run(() =>
{
    var sr = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

    while (!cts.Token.IsCancellationRequested)
    {
        string? line;

        try { line = sr.ReadLine(); }
        catch
        {
            Console.WriteLine("\nConnection broken.");
            cts.Cancel();
            break;
        }

        if (line is null)
        {
            Console.WriteLine("\nServer disconnected.");
            cts.Cancel();
            break;
        }

        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        Console.WriteLine($"[Server]: {line}");
        Console.Write("> ");
    }
});

while (!cts.Token.IsCancellationRequested)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;

    try
    {
        var bytes = Encoding.UTF8.GetBytes(input + "\n");
        stream.Write(bytes, 0, bytes.Length);
    }
    catch
    {
        Console.WriteLine("Failed to send message.");
        break;
    }

    if (input == "exit")
    {
        cts.Cancel();
        break;
    }
}

stream.Close();
client.Close();