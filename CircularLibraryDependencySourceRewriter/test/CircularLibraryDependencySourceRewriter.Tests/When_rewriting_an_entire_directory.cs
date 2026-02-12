using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static CircularLibraryDependencySourceRewriter.SourceRewriter;

// ReSharper disable InconsistentNaming

namespace CircularLibraryDependencySourceRewriter.Tests;

public class When_rewriting_an_entire_directory : IDisposable
{
   static readonly string TestProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
   static readonly string InputDir = Path.Combine(TestProjectDir, "input_source");
   static readonly string ExpectedOutputDir = Path.Combine(TestProjectDir, "output_source");
   static readonly string BatchOutputDir = Path.Combine(TestProjectDir, "batch_output_source");

   public When_rewriting_an_entire_directory() => RewriteDirectory(InputDir, BatchOutputDir);

   public void Dispose()
   {
      if(Directory.Exists(BatchOutputDir))
         Directory.Delete(BatchOutputDir, recursive: true);
   }

   static string[] RelativeFiles(string dir) =>
      Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
               .Select(f => Path.GetRelativePath(dir, f))
               .Order()
               .ToArray();

   [XF] public void the_output_contains_the_same_files_as_expected() =>
      string.Join("\n", RelativeFiles(BatchOutputDir)).Must().Be(string.Join("\n", RelativeFiles(ExpectedOutputDir)));

   [XF] public void every_file_matches_the_expected_output()
   {
      foreach(var relativePath in RelativeFiles(ExpectedOutputDir))
      {
         var expected = File.ReadAllText(Path.Combine(ExpectedOutputDir, relativePath));
         var actual = File.ReadAllText(Path.Combine(BatchOutputDir, relativePath));
         actual.Must().Be(expected);
      }
   }
}
