using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.v3.BDD;
#pragma warning disable CA1813 //avoid unsealed attributes

/// <summary>
/// Exclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(typeof(XFactDiscoverer))]
public class XFactAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
   FactAttribute(sourceFilePath, sourceLineNumber)
{}

/// <summary>
/// Short alias for <see cref="XFactAttribute"/>
/// Exclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
public sealed class XFAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : XFactAttribute(sourceFilePath, sourceLineNumber) {}

#pragma warning restore CA1813 //avoid unsealed attributes
