
using Microsoft.AspNetCore.Identity;
using SchoolManagement.Infrastructure.Persistence.Entities;

Console.Write("Enter new administrator password: ");

var characters = new List<char>();

while (true)
{
    var key = Console.ReadKey(intercept: true);

    if (key.Key == ConsoleKey.Enter)
        break;

    if (key.Key == ConsoleKey.Backspace)
    {
        if (characters.Count > 0)
            characters.RemoveAt(characters.Count - 1);

        continue;
    }

    if (!char.IsControl(key.KeyChar))
        characters.Add(key.KeyChar);
}

string password = new string(characters.ToArray());
characters.Clear();

Console.WriteLine();

if (password.Length < 12 || password.Length > 128)
{
    Console.WriteLine("Password must contain 12 to 128 characters.");
    return;
}

var user = new User
{
    UserId = 1,
    Username = "tongbora",
    FullName = "Administrator"
};

var hasher = new PasswordHasher<User>();

string hash = hasher.HashPassword(user, password);

Console.WriteLine();
Console.WriteLine("Generated PasswordHash:");
Console.WriteLine(hash);
