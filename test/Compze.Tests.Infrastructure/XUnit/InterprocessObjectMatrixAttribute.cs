using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

namespace Compze.Tests.Infrastructure.XUnit;

public sealed class InterprocessObjectMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<InterprocessObjectBackingStore>(
      configurationFileName: null,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static InterprocessObjectBackingStore BackingStore => CurrentComponent1;
}
