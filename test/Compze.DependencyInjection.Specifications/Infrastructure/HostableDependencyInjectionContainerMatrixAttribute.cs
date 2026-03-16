using System.Runtime.CompilerServices;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.xUnitMatrix;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

sealed class HostableDependencyInjectionContainerMatrixAttribute : MatrixTheoryAttribute<DIContainer>
{
   public HostableDependencyInjectionContainerMatrixAttribute([CallerFilePath] string? sourceFilePath = null,
                                                              [CallerLineNumber] int sourceLineNumber = -1) : base(configurationFileName: null,
                                                                                                                   useTestMethodArgument: false,
                                                                                                                   sourceFilePath: sourceFilePath,
                                                                                                                   sourceLineNumber: sourceLineNumber)
   {
      SkipValues(DIContainer.SimpleInjector, "SimpleInjector does not support IServiceProviderIsService needed for ASP.NET Core hosting");
   }
}
