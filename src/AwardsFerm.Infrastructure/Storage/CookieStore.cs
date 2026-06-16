using System.Text.Json;
using Microsoft.Playwright;

namespace AwardsFerm.Infrastructure.Storage;

public interface ICookieStore
{
    Task LoadAsync(IBrowserContext context, string cookiesPath, CancellationToken cancellationToken = default);
    Task SaveAsync(IBrowserContext context, string cookiesPath, CancellationToken cancellationToken = default);
}

public sealed class CookieStore : ICookieStore
{
    public async Task LoadAsync(IBrowserContext context, string cookiesPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(cookiesPath))
            return;

        var json = await File.ReadAllTextAsync(cookiesPath, cancellationToken);
        var cookies = JsonSerializer.Deserialize<List<Cookie>>(json);
        if (cookies is { Count: > 0 })
            await context.AddCookiesAsync(cookies);
    }

    public async Task SaveAsync(IBrowserContext context, string cookiesPath, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(cookiesPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var cookies = await context.CookiesAsync();
        var json = JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(cookiesPath, json, cancellationToken);
    }
}
