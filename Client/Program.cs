using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 5000;

PrintHeader();

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
            PrintEvent("Connection broken.");
            cts.Cancel();
            break;
        }

        if (line is null)
        {
            PrintEvent("Server disconnected.");
            cts.Cancel();
            break;
        }

        PrintMessage(line);
        PrintPrompt();
    }
});

while (!cts.Token.IsCancellationRequested)
{
    PrintPrompt();
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;

    try
    {
        var bytes = Encoding.UTF8.GetBytes(input + "\n");
        stream.Write(bytes, 0, bytes.Length);
    }
    catch
    {
        PrintError("Failed to send message.");
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

void PrintHeader()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine("  ╔════════════════════════════════════════╗");
    Console.WriteLine("  ║           💬  TCP Chat  v1.0            ║");
    Console.WriteLine("  ╚════════════════════════════════════════╝");
    Console.ResetColor();
    PrintSeparator();
}

void PrintSeparator()
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ─────────────────────────────────────────");
    Console.ResetColor();
}

void PrintPrompt()
{
    Console.ForegroundColor = ConsoleColor.DarkGreen;
    Console.Write("\n  > ");
    Console.ResetColor();
}

void PrintMessage(string line)
{
    var time = DateTime.Now.ToString("HH:mm");

    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

    if (line.StartsWith(">>"))
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  {time}  {line}");
        PrintSeparator();
    }
    else if (line.StartsWith("[") && line.Contains("]:"))
    {
        var nameEnd = line.IndexOf("]:");
        var name = line[1..nameEnd];
        var content = line[(nameEnd + 2)..].Trim();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {time}  ");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{name,-16}");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("│  ");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(content);
    }
    else if (line.StartsWith("Online"))
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {time}  ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(line);
    }
    else if (line.StartsWith("  •"))
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"         {line}");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {time}  ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("⚙  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(line);
    }

    Console.ResetColor();
}

void PrintError(string message)
{
    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  {DateTime.Now:HH:mm}  ✖  {message}");
    Console.ResetColor();
}

void PrintEvent(string message)
{
    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($"  {DateTime.Now:HH:mm}  ⚡  {message}");
    Console.ResetColor();
}