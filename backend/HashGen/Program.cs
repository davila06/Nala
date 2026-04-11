// ── HashGen ── local developer utility to generate BCrypt hashes ─────────────
// NEVER hardcode passwords here. Pass them via stdin or an env variable.
//
// Usage:
//   dotnet run --project HashGen           (prompts securely)
//   HASH_INPUT="mypassword" dotnet run     (env var — avoid in shell history)
//
// The resulting hash can be safely stored in SQL seed scripts or user-secrets.
// ─────────────────────────────────────────────────────────────────────────────

const int WorkFactor = 12; // OWASP 2026 recommendation

var input = Environment.GetEnvironmentVariable("HASH_INPUT");

if (string.IsNullOrWhiteSpace(input))
{
    Console.Write("Enter password to hash (input hidden): ");
    input = ReadPasswordFromConsole();
    Console.WriteLine();
}

if (string.IsNullOrWhiteSpace(input))
{
    Console.Error.WriteLine("No input provided. Exiting.");
    return 1;
}

var hash = BCrypt.Net.BCrypt.HashPassword(input, WorkFactor);
Console.WriteLine(hash);
return 0;

static string ReadPasswordFromConsole()
{
    var password = new System.Text.StringBuilder();
    ConsoleKeyInfo key;

    do
    {
        key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Remove(password.Length - 1, 1);
        }
        else if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
        {
            password.Append(key.KeyChar);
        }
    }
    while (key.Key != ConsoleKey.Enter);

    return password.ToString();
}

