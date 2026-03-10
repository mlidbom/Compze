using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

#pragma warning disable CA1813 //We create inheritable attributes. So shoot us.

namespace Compze.Tests.Infrastructure.XUnit;

public class PCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<SqlLayer, DIContainer, Serializer, Transport>(
      configurationFileName: "TestUsingPluggableComponentCombinations",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static SqlLayer SqlLayer => CurrentComponent1;
   public static DIContainer DIContainer => CurrentComponent2;
   public static Serializer Serializer => CurrentComponent3;
   public static Transport Transport => CurrentComponent4;
}
