using System.Runtime.CompilerServices;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitBDD;

/// <summary>
/// Short alias for <see cref="ExclusiveFactAttribute"/>
/// Exclusive Fact attribute.
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
/// <remarks>
/// An abstract class's test runs only in the classes derived from it, since xUnit never runs an abstract class.<br/>
/// So a fact with this attribute that is declared on an abstract class would run nowhere: instead, each derived class<br/>
/// lists it as a test that fails, saying so. Use <see cref="Xunit.FactAttribute"/> for a fact to run in every derived class.
/// </remarks>
public sealed class XFAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ExclusiveFactAttribute(sourceFilePath, sourceLineNumber);
