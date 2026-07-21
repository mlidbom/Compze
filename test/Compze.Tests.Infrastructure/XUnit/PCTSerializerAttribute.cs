using System.Runtime.CompilerServices;
using Compze.Hosting.Testing;
using Compze.xUnitMatrix;

namespace Compze.Tests.Infrastructure.XUnit;

public sealed class PCTSerializerAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer>(
      configurationFileName: null,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static Serializer Serializer => CurrentDimensionValue1;
}
