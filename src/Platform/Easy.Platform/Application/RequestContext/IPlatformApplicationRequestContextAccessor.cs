using System.Diagnostics.CodeAnalysis;

namespace Easy.Platform.Application.RequestContext;

/// <summary>
/// This is a singleton object help to access the current UserContext.
/// </summary>
public interface IPlatformApplicationRequestContextAccessor
{
    [NotNull]
    IPlatformApplicationRequestContext Current { get; set; }
}
