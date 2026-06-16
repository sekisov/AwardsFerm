namespace AwardsFerm.Api.Services;

internal static class ProfilesPathHelper
{
    public static string FindProfilesRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "profiles");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "profiles");
    }
}
