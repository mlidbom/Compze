using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.InternalizedSourceReferences.SourceRewriter;

// ReSharper disable InconsistentNaming

namespace Compze.InternalizedSourceReferences.Tests;

public class When_rewriting_multiple_directories : IDisposable
{
   static readonly string TestProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
   static readonly string InputUtilitiesDir = Path.Combine(TestProjectDir, "input_source", "Compze.Utilities");
   static readonly string ExpectedUtilitiesOutputDir = Path.Combine(TestProjectDir, "output_source", "Compze.Utilities");
   static readonly string MultiOutputDir = Path.Combine(TestProjectDir, "multi_output_source");

   // Split the input into two separate directories: Functional + everything else
   static readonly string SplitDir = Path.Combine(TestProjectDir, "multi_input_split");
   static readonly string SplitDirA = Path.Combine(SplitDir, "A");
   static readonly string SplitDirB = Path.Combine(SplitDir, "B");

   public When_rewriting_multiple_directories()
   {
      // Clean up from any previous run
      if(Directory.Exists(SplitDir)) Directory.Delete(SplitDir, recursive: true);
      if(Directory.Exists(MultiOutputDir)) Directory.Delete(MultiOutputDir, recursive: true);

      // Copy Functional/ into split A, everything else into split B
      CopySubdirectory("Functional", SplitDirA);
      foreach(var subDir in Directory.GetDirectories(InputUtilitiesDir))
      {
         var name = Path.GetFileName(subDir);
         if(name != "Functional")
            CopySubdirectory(name, SplitDirB);
      }

      RewriteDirectories([SplitDirA, SplitDirB], MultiOutputDir);
   }

   static void CopySubdirectory(string subDirName, string targetRoot)
   {
      var sourceDir = Path.Combine(InputUtilitiesDir, subDirName);
      var targetDir = Path.Combine(targetRoot, subDirName);

      foreach(var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
      {
         var relativePath = Path.GetRelativePath(sourceDir, file);
         var targetFile = Path.Combine(targetDir, relativePath);
         Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
         File.Copy(file, targetFile);
      }
   }

   public void Dispose()
   {
      if(Directory.Exists(SplitDir)) Directory.Delete(SplitDir, recursive: true);
      if(Directory.Exists(MultiOutputDir)) Directory.Delete(MultiOutputDir, recursive: true);
   }

   static string[] RelativeFiles(string dir) =>
      Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
               .Select(f => Path.GetRelativePath(dir, f))
               .Order()
               .ToArray();

   [XF] public void the_output_contains_the_same_files_as_rewriting_the_single_directory() =>
      string.Join("\n", RelativeFiles(MultiOutputDir)).Must().Be(string.Join("\n", RelativeFiles(ExpectedUtilitiesOutputDir)));

   [XF] public void every_file_matches_the_single_directory_output()
   {
      foreach(var relativePath in RelativeFiles(ExpectedUtilitiesOutputDir))
      {
         var expected = File.ReadAllText(Path.Combine(ExpectedUtilitiesOutputDir, relativePath));
         var actual = File.ReadAllText(Path.Combine(MultiOutputDir, relativePath));
         actual.Must().Be(expected);
      }
   }
}
