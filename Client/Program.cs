using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 5000;

var client = new TcpClient();
client.Connect(IPAddress.Loopback, port);

var stream = client.GetStream();

var reader = Task.Run(() =>
{
    var buffer = new byte[1024];

    while (true)
    {
        int bytesRead;

        try
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);
        }
        catch
        {
            Console.WriteLine("Connection broken.");
            break;
        }

        if (bytesRead == 0)
        {
            Console.WriteLine("Server disconnected.");
            break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.WriteLine();
        Console.WriteLine($"Server: {message}");
        Console.Write("Client: ");
    }
});

while (true)
{
    Console.Write("Client: ");
    var message = Console.ReadLine();

    var messageBytes = Encoding.UTF8.GetBytes(message!);
    stream.Write(messageBytes, 0, messageBytes.Length);

    if (message == "exit")
    {
        Console.WriteLine("Chat ended.");
        break;
    }
}

stream.Close();
client.Close();