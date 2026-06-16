using AwardsFerm.Core.Models;

namespace AwardsFerm.Core.Interfaces;

public interface IProfileRepository
{
    Task<DesktopProfile> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<DesktopProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
