using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure;

public static class UniversalTestStartup
{
   [ModuleInitializer]
   public static void Initialize() => FluentAssertions.License.Accepted = true;
}