using FluentAssertions;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(ScratchPad.Internals.FluentAssertionsInitializer),
    nameof(ScratchPad.Internals.FluentAssertionsInitializer.SuppressLicenseWarning))]

namespace ScratchPad.Internals;

public static class FluentAssertionsInitializer
{
    public static void SuppressLicenseWarning()
    {
        License.Accepted = true;
    }
}
