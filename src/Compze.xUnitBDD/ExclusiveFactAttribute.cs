using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;
using Compze.xUnitBDD.Private;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitBDD;

/// <summary>
/// Exclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(typeof(ExclusiveFactDiscoverer))]
public class ExclusiveFactAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
   FactAttribute(sourceFilePath, sourceLineNumber);
