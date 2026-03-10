using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

sealed class HostableDependencyInjectionContainerMatrixAttribute : ComponentCombinationsTheoryAttribute<DIContainer>
{
   public HostableDependencyInjectionContainerMatrixAttribute([CallerFilePath] string? sourceFilePath = null,
                                                              [CallerLineNumber] int sourceLineNumber = -1) : base(configurationFileName: null,
                                                                                                                   useTestMethodArgument: false,
                                                                                                                   sourceFilePath: sourceFilePath,
                                                                                                                   sourceLineNumber: sourceLineNumber)
   {
      Skipped = [DIContainer.SimpleInjector];
   }
}
