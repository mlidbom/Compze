using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(Compze.Tests.Infrastructure.XUnit.FluentAssertionsInitializer),
    nameof(Compze.Tests.Infrastructure.XUnit.FluentAssertionsInitializer.SuppressLicenseWarning))]

namespace Compze.Tests.Infrastructure.XUnit;

public static class FluentAssertionsInitializer
{
    public static void SuppressLicenseWarning()
    {
        License.Accepted = true;
    }
}
