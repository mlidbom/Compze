using System.Runtime.CompilerServices;
using Compze.xUnitMatrix.Tests._2Components;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix.Tests._6Components;

// More than five dimensions: inherit the non-generic base directly, passing the dimension enum types, and read each
// value with GetCurrentDimensionValue. This is the path the README documents for 6+ dimensions.
sealed class SixComponentMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute(
      configurationFileName: null,
      dimensionEnumTypes: [typeof(Serializer), typeof(SqlLayer), typeof(DIContainer), typeof(TeventStore), typeof(TessageBus), typeof(Transport)],
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber)
{
   public static Serializer Serializer => GetCurrentDimensionValue<Serializer>(0);
   public static SqlLayer SqlLayer => GetCurrentDimensionValue<SqlLayer>(1);
   public static DIContainer DIContainer => GetCurrentDimensionValue<DIContainer>(2);
   public static TeventStore TeventStore => GetCurrentDimensionValue<TeventStore>(3);
   public static TessageBus TessageBus => GetCurrentDimensionValue<TessageBus>(4);
   public static Transport Transport => GetCurrentDimensionValue<Transport>(5);
}
