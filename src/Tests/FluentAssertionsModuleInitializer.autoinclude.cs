using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Tests;

// This file is automatically included in all test projects via Directory.Build.props
// No need to copy it to each project!
public static class FluentAssertionsModuleInitializer
{
   [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Shared test infrastructure needs module initialization to suppress FluentAssertions license message across all test projects")]
   [ModuleInitializer]
   public static void Initialize()
   {
      FluentAssertions.License.Accepted = true;
      FluentAssertions.AssertionConfiguration.Current.Formatting.MaxLines = int.MaxValue;
   }
}
