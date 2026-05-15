using Server.Models;
using Server.Repositories;

namespace Server.Services;

public class AuthService
{
    private readonly IUserRepository _repository;
    private readonly PasswordHasher _passwordHasher;

    public AuthService(IUserRepository repository, PasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public (bool Success, string Message, User? User) Register(string username, string displayName, string password)
    {
        if (_repository.Exists(username))
            return (false, "Username already taken.", null);

        var user = new User
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.Hash(password)
        };

        _repository.Add(user);
        return (true, $"Registered successfully! Welcome, {displayName}.", user);
    }

    public (bool Success, string Message, User? User) Login(string username, string password)
    {
        var user = _repository.GetByUsername(username);

        if (user is null)
            return (false, "User not found.", null);

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            return (false, "Invalid password.", null);

        user.LastLoginAt = DateTime.UtcNow;
        _repository.Update(user);

        return (true, $"Welcome back, {user.DisplayName}!", user);
    }
}