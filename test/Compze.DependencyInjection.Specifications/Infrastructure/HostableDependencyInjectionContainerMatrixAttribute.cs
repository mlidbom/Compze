using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

sealed class HostableDependencyInjectionContainerMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute<DIContainer>(
      configurationFileName: "TestUsingHostableDependencyInjectionContainers",
      useTestMethodArgument: false,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
