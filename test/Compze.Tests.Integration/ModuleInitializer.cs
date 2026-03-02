using System.Runtime.CompilerServices;

namespace Compze.Tests.Integration;

// This file is automatically included in all test projects via Directory.Build.props
// No need to copy it to each project!
public static class IntegrationTestsModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      //We just need to force the Compze.Tests.Infrastructure assembly to load so that the initializers in that assembly runs
      // ReSharper disable once UnusedVariable
      var ignored = typeof(Compze.Tests.Infrastructure.TestFixtureHelper);
   }
}
