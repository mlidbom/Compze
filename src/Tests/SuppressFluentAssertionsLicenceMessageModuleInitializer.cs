using System.Runtime.CompilerServices;

namespace Compze.Tests;

// This file is automatically included in all test projects via Directory.Build.props
// No need to copy it to each project!
public static class SuppressFluentAssertionsLicenceMessageModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize() => FluentAssertions.License.Accepted = true;
}
