using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 5000;

var client = new TcpClient();
client.Connect(IPAddress.Loopback, port);

Console.WriteLine("Connected to server.");

var stream = client.GetStream();
var cts = new CancellationTokenSource();

var reader = Task.Run(() =>
{
    var buffer = new byte[1024];

    while (!cts.Token.IsCancellationRequested)
    {
        int bytesRead;

        try
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);
        }
        catch
        {
            Console.WriteLine("\nConnection broken.");
            cts.Cancel();
            break;
        }

        if (bytesRead == 0)
        {
            Console.WriteLine("\nServer disconnected.");
            cts.Cancel();
            break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        Console.WriteLine($"[Server]: {message}");
        Console.Write("> ");
    }
});

while (!cts.Token.IsCancellationRequested)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    try
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        stream.Write(bytes, 0, bytes.Length);
    }
    catch
    {
        Console.WriteLine("Failed to send message.");
        break;
    }

    if (input == "exit")
    {
        Console.WriteLine("Disconnecting...");
        cts.Cancel();
        break;
    }
}

stream.Close();
client.Close();