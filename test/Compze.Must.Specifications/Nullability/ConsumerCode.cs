using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Compze.Must.Specifications.Nullability;

/// <summary>
/// Code a consumer of Compze.Must writes, compiled the way a consuming project compiles it: with nullable reference types<br/>
/// enabled, against the Compze.Must assembly under specification and the .NET libraries this process runs on.
/// </summary>
/// <remarks>
/// Compze.Must's signatures decide which of a consumer's assertions compile without warnings, and a signature that decides<br/>
/// wrongly shows only in the consumer's build: the specifications of what assertions do at run time can't see it.<br/>
/// Compiling consumer code here is how a specification observes it.
/// </remarks>
sealed class ConsumerCode
{
   static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(ReferencesToCompzeMustAndTheDotNetLibraries);

   readonly IReadOnlyList<Diagnostic> _warningsAndErrors;

   ConsumerCode(IReadOnlyList<Diagnostic> warningsAndErrors) => _warningsAndErrors = warningsAndErrors;

   /// <summary>The warnings and errors compiling the code gave, each as the compiler reports it: its location, id and message.</summary>
   public IReadOnlyList<string> WarningsAndErrors => [.._warningsAndErrors.Select(it => it.ToString())];

   /// <summary>The ids of the <see cref="WarningsAndErrors"/>, such as <c>CS8602</c>.</summary>
   public IReadOnlyList<string> WarningAndErrorIds => [.._warningsAndErrors.Select(it => it.Id)];

   /// <summary>
   /// Compiles <paramref name="statements"/> as the body of a method that takes <paramref name="parameters"/>, such as<br/>
   /// <c>string? subject</c>. A parameter, unlike a local, starts out as possibly null whenever its type says so.
   /// </summary>
   public static ConsumerCode Compiling(string parameters, string statements)
   {
      var source = $$"""
                     using System;
                     using System.Collections.Generic;
                     using System.Collections.ObjectModel;
                     using Compze.Must;

                     static class Consumer
                     {
                        static void Asserting({{parameters}})
                        {
                           {{statements}}
                        }
                     }
                     """;

      var compilation = CSharpCompilation.Create(assemblyName: "Consumer",
                                                 [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
                                                 References.Value,
                                                 new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

      return new ConsumerCode([..compilation.GetDiagnostics().Where(it => it.Severity >= DiagnosticSeverity.Warning)]);
   }

   /// <summary>Compze.Must, the assemblies it references, and the managed assemblies the runtime trusts from its own directory.</summary>
   static IReadOnlyList<MetadataReference> ReferencesToCompzeMustAndTheDotNetLibraries()
   {
      var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
      var dotNetLibraries = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                                                                                      .Where(path => string.Equals(Path.GetDirectoryName(path), runtimeDirectory, StringComparison.OrdinalIgnoreCase));
      var compzeMust = typeof(MustExtensions).Assembly;
      var compzeMustAndTheAssembliesItReferences = compzeMust.GetReferencedAssemblies()
                                                             .Select(Assembly.Load)
                                                             .Append(compzeMust)
                                                             .Select(it => it.Location);

      return [..dotNetLibraries.Concat(compzeMustAndTheAssembliesItReferences)
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .Select(path => MetadataReference.CreateFromFile(path))];
   }
}
