using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._4Components;

sealed class FourComponentMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<Serializer, SqlLayer, DIContainer, TeventStore>(
      configurationFileName: null,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static Serializer Serializer => CurrentDimensionValue1;
   public static SqlLayer SqlLayer => CurrentDimensionValue2;
   public static DIContainer DIContainer => CurrentDimensionValue3;
   public static TeventStore TeventStore => CurrentDimensionValue4;
}
