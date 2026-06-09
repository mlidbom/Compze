using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

sealed class DependencyInjectionContainerMatrixAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : MatrixTheoryAttribute<DIContainer>(
      configurationFileName: null,
      sourceFilePath: sourceFilePath,
      sourceLineNumber: sourceLineNumber);
