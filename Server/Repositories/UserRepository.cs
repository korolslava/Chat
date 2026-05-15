using System.Text.Json;
using Server.Models;

namespace Server.Repositories;

public class UserRepository : IUserRepository
{
    private const string FilePath = "../../../Data/users.json";
    private readonly List<User> _users;

    public UserRepository()
    {
        _users = Load();
    }

    public User? GetByUsername(string username) =>
        _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public bool Exists(string username) =>
        _users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public void Add(User user)
    {
        _users.Add(user);
        Save();
    }

    public void Update(User user)
    {
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index != -1)
        {
            _users[index] = user;
            Save();
        }
    }

    private List<User> Load()
    {
        if (!File.Exists(FilePath))
            return [];

        var json = File.ReadAllText(FilePath);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<User>>(json) ?? [];
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}