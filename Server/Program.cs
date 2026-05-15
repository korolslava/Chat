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
var clients = new ConcurrentDictionary<Guid, (TcpClient Client, User User)>();

var repository = new UserRepository();
var passwordHasher = new PasswordHasher();
var authService = new AuthService(repository, passwordHasher);

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
    Console.WriteLine("Client connected. Waiting for auth...");

    var stream = client.GetStream();
    User? authenticatedUser = null;

    try
    {
        SendToClient(stream, "Welcome to the chat!");
        SendToClient(stream, "Use /register <username> <displayName> <password>");
        SendToClient(stream, "Use /login <username> <password>");

        while (authenticatedUser is null)
        {
            var input = ReadFromClient(stream);

            if (input is null)
                return;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                SendToClient(stream, "Unknown command.");
                continue;
            }

            var command = parts[0].ToLower();

            if (command == "/register" && parts.Length == 4)
            {
                var (success, msg, user) = authService.Register(parts[1], parts[2], parts[3]);
                SendToClient(stream, msg);

                if (success)
                    authenticatedUser = user;
            }
            else if (command == "/login" && parts.Length == 3)
            {
                var (success, msg, user) = authService.Login(parts[1], parts[2]);
                SendToClient(stream, msg);

                if (success)
                    authenticatedUser = user;
            }
            else
            {
                SendToClient(stream, "Please login or register first.");
            }
        }

        var clientId = Guid.NewGuid();
        clients.TryAdd(clientId, (client, authenticatedUser));

        Console.WriteLine($"{authenticatedUser.DisplayName} ({authenticatedUser.Username}) joined.");
        Console.Write("Server: ");

        SendToClient(stream, "You are now in the chat! Type /online to see who's online.");

        while (true)
        {
            var input = ReadFromClient(stream);

            if (input is null)
                break;

            if (input == "/online")
            {
                var onlineList = string.Join(Environment.NewLine,
                    clients.Values.Select(c => $"  {c.User.DisplayName} ({c.User.Username})"));

                SendToClient(stream, $"Online:{Environment.NewLine}{onlineList}");
            }
            else
            {
                SendToClient(stream, "Unknown command.");
            }
        }

        clients.TryRemove(clientId, out _);
        Console.WriteLine($"{authenticatedUser.DisplayName} disconnected.");
        Console.Write("Server: ");
    }
    catch
    {
        if (authenticatedUser is not null)
        {
            Console.WriteLine($"{authenticatedUser.DisplayName} disconnected unexpectedly.");
            Console.Write("Server: ");
        }
    }
}

void SendToClient(NetworkStream stream, string message)
{
    var data = Encoding.UTF8.GetBytes(message);
    stream.Write(data, 0, data.Length);
}

string? ReadFromClient(NetworkStream stream)
{
    var buffer = new byte[1024];
    var read = stream.Read(buffer, 0, buffer.Length);

    if (read == 0)
        return null;

    return Encoding.UTF8.GetString(buffer, 0, read);
}