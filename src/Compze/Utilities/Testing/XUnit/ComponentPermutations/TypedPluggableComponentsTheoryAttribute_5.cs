using Compze.Utilities.Functional;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public abstract class PluggableComponentsTheoryAttribute<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5> : PluggableComponentsTheoryAttribute
   where TComponent1 : Enum
   where TComponent2 : Enum
{
   protected PluggableComponentsTheoryAttribute(string configurationFileName,
                                                object[]? skipped,
                                                string[]? skipReasons,
                                                bool useTestMethodArgument,
                                                string? sourceFilePath,
                                                int sourceLineNumber)
      : base(configurationFileName: configurationFileName,
             componentEnumTypes: EnumerableCE.OfTypes<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5>().ToArray(),
             skipped: skipped,
             skipReasons: skipReasons,
             useTestMethodArgument: useTestMethodArgument,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber) {}
}
