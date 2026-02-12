using Compze.Utilities.Testing.Must;
using Xunit;
using static CircularLibraryDependencySourceRewriter.SourceRewriter;

// ReSharper disable InconsistentNaming

namespace CircularLibraryDependencySourceRewriter.Tests;

public class When_rewriting_each_source_file
{
   static readonly string TestProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
   static readonly string InputDir = Path.Combine(TestProjectDir, "input_source");
   static readonly string OutputDir = Path.Combine(TestProjectDir, "output_source");

   public static TheoryData<string> SourceFiles()
   {
      var data = new TheoryData<string>();
      foreach(var file in Directory.EnumerateFiles(InputDir, "*.cs", SearchOption.AllDirectories))
         data.Add(Path.GetRelativePath(InputDir, file));
      return data;
   }

   [Theory]
   [MemberData(nameof(SourceFiles))]
   public void the_rewritten_output_matches_the_expected_file(string relativePath)
   {
      var input = File.ReadAllText(Path.Combine(InputDir, relativePath));
      var expected = File.ReadAllText(Path.Combine(OutputDir, relativePath));

      MakeTypesInternal(input).Must().Be(expected);
   }
}
