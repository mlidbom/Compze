using Compze.Utilities.Functional;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public abstract class PluggableComponentsTheoryAttribute<TComponent1> : PluggableComponentsTheoryAttribute
   where TComponent1 : Enum
{
   protected PluggableComponentsTheoryAttribute(string configurationFileName,
                                                object[]? skipped,
                                                string[]? skipReasons,
                                                bool useTestMethodArgument,
                                                string? sourceFilePath,
                                                int sourceLineNumber)
      : base(configurationFileName: configurationFileName,
             componentEnumTypes:EnumerableCE.OfTypes<TComponent1>().ToArray(),
             skipped: skipped,
             skipReasons: skipReasons,
             useTestMethodArgument: useTestMethodArgument,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber) {}
}
