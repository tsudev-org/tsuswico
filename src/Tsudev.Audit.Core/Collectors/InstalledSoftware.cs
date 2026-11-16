using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Mot phan mem da cai dat, doc tu registry Uninstall.</summary>
public sealed record InstalledSoftware(
    string Name, string Version, string Publisher, string InstallDate,
    string InstallLocation, string Category, string ProductGuid)
{
    /// <summary>Thieu Publisher hoac Version -> can kiem tra thu cong.</summary>
    public bool NeedsManualReview =>
        Publisher is "-" or "" || Version is "-" or "";
}
