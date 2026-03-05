using System.Runtime.CompilerServices;
using Compze.Core.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

#pragma warning disable CA1813 //We create inheritable attributes. So shoot us.

namespace Compze.Tests.Infrastructure.XUnit;

public class PCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<SqlLayer, DIContainer, Serializer, Transport>(
      configurationFileName: "TestUsingPluggableComponentCombinations",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

public sealed class PCTSerializerAttribute : PCTAttribute
{
   public PCTSerializerAttribute([CallerFilePath] string? sourceFilePath = null,
                                 [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath: sourceFilePath,
                                                                                      sourceLineNumber: sourceLineNumber) =>
      OnlyConsider = typeof(Serializer);
}

// ReSharper disable once InconsistentNaming
public sealed class PCTDIContainerAttribute : PCTAttribute
{
   public PCTDIContainerAttribute([CallerFilePath] string? sourceFilePath = null,
                                  [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath: sourceFilePath,
                                                                                       sourceLineNumber: sourceLineNumber) =>
      OnlyConsider = typeof(DIContainer);
}
