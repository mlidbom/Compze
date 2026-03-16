using Xunit;

namespace Compze.Threading.Specifications.TestInfrastructure;

enum CancellationMechanism
{
   ThreadInterrupt,
   CancellationToken
}

static class CancellationMechanismExtensions
{
   internal static void SkipCancellationTokenUntilImplemented(this CancellationMechanism mechanism) =>
      Assert.SkipWhen(mechanism == CancellationMechanism.CancellationToken, "CancellationToken parameter not yet added to the interface");
}
