using System.Runtime.CompilerServices;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public abstract class PluggableComponentsTheoryAttribute<TComponent1, TComponent2> : PluggableComponentsTheoryAttribute
   where TComponent1 : Enum
   where TComponent2 : Enum
{
   protected PluggableComponentsTheoryAttribute(
      object[]? skippedComponents = null,
      string[]? skipReasons = null,
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
      : base([
                typeof(TComponent1),
                typeof(TComponent2)
             ],
             skippedComponents,
             skipReasons,
             sourceFilePath,
             sourceLineNumber) {}
}
