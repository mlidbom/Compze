using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
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

public sealed class PCTSerializerAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<Serializer>(
      configurationFileName: null,
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);

