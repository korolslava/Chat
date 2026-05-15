using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Models;
using Server.Repositories;
using Server.Services;

const int port = 5000;

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();
Console.WriteLine($"Server started on {IPAddress.Loopback}:{port}");

var cts = new CancellationTokenSource();

var clients = new ConcurrentDictionary<Guid, (TcpClient Client, User User, NetworkStream Stream)>();

var repository = new UserRepository();
var passwordHasher = new PasswordHasher();
var authService = new AuthService(repository, passwordHasher);

Task.Run(() =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        var client = listener.AcceptTcpClient();
        Task.Run(() => HandleClient(client));
    }
}, cts.Token);

Console.WriteLine("Type 'exit' to stop the server.");
while (Console.ReadLine() != "exit") { }

cts.Cancel();
listener.Stop();
Console.WriteLine("Server stopped.");

void HandleClient(TcpClient client)
{
    var stream = client.GetStream();

    var sr = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

    User? authenticatedUser = null;
    var clientId = Guid.NewGuid();

    Console.WriteLine("New client connected, waiting for auth...");

    try
    {
        SendToClient(stream, "Welcome to the chat!");
        SendToClient(stream, "Use /register <username> <displayName> <password>");
        SendToClient(stream, "Use /login <username> <password>");

        while (authenticatedUser is null)
        {
            var input = sr.ReadLine();
            if (input is null) return;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var command = parts[0].ToLower();

            if (command == "/register" && parts.Length == 4)
            {
                var (success, msg, user) = authService.Register(parts[1], parts[2], parts[3]);
                SendToClient(stream, msg);
                if (success) authenticatedUser = user;
            }
            else if (command == "/login" && parts.Length == 3)
            {
                var (success, msg, user) = authService.Login(parts[1], parts[2]);
                SendToClient(stream, msg);
                if (success) authenticatedUser = user;
            }
            else
            {
                SendToClient(stream, "Please login or register first.");
            }
        }

        clients.TryAdd(clientId, (client, authenticatedUser, stream));
        Console.WriteLine($"{authenticatedUser.DisplayName} ({authenticatedUser.Username}) joined.");

        BroadcastToAll($">> {authenticatedUser.DisplayName} joined the chat.", excludeId: clientId);
        SendToClient(stream, "You are now in the chat! Type /online to see who is online.");

        while (true)
        {
            var input = sr.ReadLine();
            if (input is null) break;
            if (string.IsNullOrWhiteSpace(input)) continue;

            if (input == "/online")
            {
                var list = string.Join("\n",
                    clients.Values.Select(c => $"  • {c.User.DisplayName} ({c.User.Username})"));
                SendToClient(stream, $"Online now:\n{list}");
            }
            else if (input.StartsWith("/"))
            {
                SendToClient(stream, $"Unknown command: {input}");
            }
            else
            {
                BroadcastToAll($"[{authenticatedUser.DisplayName}]: {input}", excludeId: null);
                Console.WriteLine($"[{authenticatedUser.DisplayName}]: {input}");
            }
        }
    }
    catch { }
    finally
    {
        clients.TryRemove(clientId, out _);

        if (authenticatedUser is not null)
        {
            Console.WriteLine($"{authenticatedUser.DisplayName} disconnected.");
            BroadcastToAll($">> {authenticatedUser.DisplayName} left the chat.", excludeId: null);
        }
    }
}

void BroadcastToAll(string message, Guid? excludeId)
{
    foreach (var (id, (_, _, clientStream)) in clients)
    {
        if (excludeId.HasValue && id == excludeId.Value) continue;
        try { SendToClient(clientStream, message); }
        catch { }
    }
}

void SendToClient(NetworkStream stream, string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");
    stream.Write(data, 0, data.Length);
}