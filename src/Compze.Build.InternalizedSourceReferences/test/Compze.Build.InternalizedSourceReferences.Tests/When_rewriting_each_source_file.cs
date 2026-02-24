using Compze.Utilities.Testing.Must;
using Xunit;
using static Compze.InternalizedSourceReferences.SourceRewriter;

// ReSharper disable InconsistentNaming

namespace Compze.InternalizedSourceReferences.Tests;

public class When_rewriting_each_source_file
{
   static readonly string TestProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
   static readonly string InputDir = Path.Combine(TestProjectDir, "input_source");
   static readonly string OutputDir = Path.Combine(TestProjectDir, "output_source");

   static readonly string[] ExcludedDirectories = ["bin", "obj", "InternalizedSource"];

   public static IEnumerable<TheoryDataRow<string>> SourceFiles()
   {
      foreach(var file in Directory.EnumerateFiles(InputDir, "*.cs", SearchOption.AllDirectories))
      {
         var relativePath = Path.GetRelativePath(InputDir, file);
         var firstSegment = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
         if(!ExcludedDirectories.Contains(firstSegment, StringComparer.OrdinalIgnoreCase))
            yield return new TheoryDataRow<string>(relativePath) { TestDisplayName = relativePath };
      }
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
