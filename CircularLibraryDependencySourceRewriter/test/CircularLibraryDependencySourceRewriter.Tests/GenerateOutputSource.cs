using Xunit;
using static CircularLibraryDependencySourceRewriter.SourceRewriter;

// ReSharper disable InconsistentNaming

namespace CircularLibraryDependencySourceRewriter.Tests;

/// <summary>
/// One-off helper to generate the output_source baseline from input_source.
/// Run with: dotnet test --filter GenerateOutputSource
/// </summary>
public class GenerateOutputSource
{
   static readonly string TestProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
   static readonly string InputDir = Path.Combine(TestProjectDir, "input_source");
   static readonly string OutputDir = Path.Combine(TestProjectDir, "output_source");

   [Fact(Skip = "Manual-only: regenerates output_source baseline. Run explicitly with: dotnet test --filter GenerateOutputSource")]
   public void generate() => RewriteDirectory(InputDir, OutputDir);
}
