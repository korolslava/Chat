using Server.Models;

namespace Server.Repositories;

public interface IUserRepository
{
    User? GetByUsername(string username);
    bool Exists(string username);
    void Add(User user);
    void Update(User user);
}