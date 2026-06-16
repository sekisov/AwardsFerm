using Microsoft.Playwright;

namespace AwardsFerm.Infrastructure.Playwright;

internal sealed class ActivePageHolder
{
    public required IBrowserContext Context { get; init; }
    public required IPage Page { get; set; }
    public string? UrlPart { get; set; }

    public IPage Resolve()
    {
        if (!Page.IsClosed)
            return Page;

        var byUrl = Context.Pages.LastOrDefault(p =>
            !p.IsClosed &&
            !string.IsNullOrEmpty(UrlPart) &&
            p.Url.Contains(UrlPart, StringComparison.OrdinalIgnoreCase));

        if (byUrl is not null)
        {
            Page = byUrl;
            return byUrl;
        }

        var anyOpen = Context.Pages.LastOrDefault(p => !p.IsClosed);
        if (anyOpen is not null)
        {
            Page = anyOpen;
            return anyOpen;
        }

        throw new PlaywrightException("Нет открытых вкладок браузера.");
    }

    public bool TryResolve(out IPage? page)
    {
        try
        {
            page = Resolve();
            return true;
        }
        catch
        {
            page = null;
            return false;
        }
    }
}
