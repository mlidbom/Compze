using System.Runtime.CompilerServices;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.BDD;

/// <summary>
/// Short alias for <see cref="ExclusiveFactAttribute"/>
/// Exclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
public sealed class XFAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ExclusiveFactAttribute(sourceFilePath, sourceLineNumber);