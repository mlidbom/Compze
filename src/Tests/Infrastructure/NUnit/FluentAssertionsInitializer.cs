using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(Compze.Tests.Infrastructure.NUnit.FluentAssertionsInitializer),
    nameof(Compze.Tests.Infrastructure.NUnit.FluentAssertionsInitializer.SuppressLicenseWarning))]

namespace Compze.Tests.Infrastructure.NUnit;

public static class FluentAssertionsInitializer
{
    public static void SuppressLicenseWarning()
    {
        License.Accepted = true;
    }
}
