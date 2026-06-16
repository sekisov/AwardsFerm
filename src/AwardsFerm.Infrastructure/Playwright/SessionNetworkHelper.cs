using Microsoft.Playwright;

namespace AwardsFerm.Infrastructure.Playwright;

internal static class SessionNetworkHelper
{
    public static async Task<string?> GetPublicIpAsync(IPage page, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await page.GotoAsync("https://api.ipify.org?format=json",
                new PageGotoOptions { Timeout = 10_000, WaitUntil = WaitUntilState.DOMContentLoaded });
            if (response is null || !response.Ok)
                return null;

            var body = await page.Locator("body").InnerTextAsync();
            if (string.IsNullOrWhiteSpace(body))
                return null;

            body = body.Trim();
            if (body.StartsWith('{'))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("ip", out var ip))
                    return ip.GetString();
            }

            return body;
        }
        catch
        {
            return null;
        }
    }
}
