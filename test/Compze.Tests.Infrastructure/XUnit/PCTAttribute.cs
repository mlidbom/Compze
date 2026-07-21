using System.Runtime.CompilerServices;
using Compze.Hosting.Testing;
using Compze.xUnitMatrix;

// ReSharper disable ExplicitCallerInfoArgument

#pragma warning disable CA1813 //We create inheritable attributes. So shoot us.

namespace Compze.Tests.Infrastructure.XUnit;

public class PCTAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<SqlLayer, DIContainer, Serializer, Transport>(
      configurationFileName: "TestUsingPluggableComponentCombinations",
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static SqlLayer SqlLayer => CurrentDimensionValue1;
   public static DIContainer DIContainer => CurrentDimensionValue2;
   public static Serializer Serializer => CurrentDimensionValue3;
   public static Transport Transport => CurrentDimensionValue4;
}
