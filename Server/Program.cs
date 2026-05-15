using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 5000;

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();

Console.WriteLine($"Server started on {IPAddress.Loopback}:{port}");

var cts = new CancellationTokenSource();

var clients = new ConcurrentDictionary<Guid, TcpClient>();

Task.Run(() =>
{
    while (true)
    {
        var client = listener.AcceptTcpClient();
        Task.Run(() => HandleClient(client));
    }
}, cts.Token);

Console.Write("Server: ");
var message = Console.ReadLine();

if (message == "exit")
{
    Console.WriteLine("Chat ended.");
    cts.Cancel();
}

listener.Stop();

void HandleClient(TcpClient client)
{
    Console.WriteLine();
    Console.WriteLine("Client Connected.");
    Console.Write("Server: ");

    clients.TryAdd(Guid.NewGuid(), client);

    try
    {
        var stream = client.GetStream();

        SendToClient(stream, "Welcome to the chat!");
        SendToClient(stream, "Use: '/online' to see who's online.");

        while (true)
        {
            var response = ReadFromClient(stream);

            if (response == "/online")
            {
                SendToClient(stream, $"Online: {Environment.NewLine}{string.Join(Environment.NewLine, clients.Keys)}");
            }

            
        }
    }
    catch
    {

    }
}

void SendToClient(NetworkStream stream, string message)
{
    var data = Encoding.UTF8.GetBytes(message);
    stream.Write(data, 0, data.Length);
}

string ReadFromClient(NetworkStream stream)
{
    var buffer = new byte[1024];

    var read = stream.Read(buffer, 0, buffer.Length);

    if (read == 0)
    {
        return null;
    }

    return Encoding.UTF8.GetString(buffer, 0, read);
}