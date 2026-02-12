using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CircularLibraryDependencySourceRewriter;

public class RewriteDirectoryTask : Microsoft.Build.Utilities.Task
{
   [Required]
   public string InputDirectories { get; set; } = "";

   [Required]
   public string OutputDirectory { get; set; } = "";

   public override bool Execute()
   {
      try
      {
         var inputs = InputDirectories.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

         Log.LogMessage(MessageImportance.Normal,
            $"CircularLibraryDependencySourceRewriter: Rewriting [{string.Join(", ", inputs.Select(i => $"'{i}'"))}] → '{OutputDirectory}'");

         SourceRewriter.RewriteDirectories(inputs, OutputDirectory);

         Log.LogMessage(MessageImportance.Normal,
            "CircularLibraryDependencySourceRewriter: Rewrite complete.");

         return true;
      }
      catch(Exception ex)
      {
         Log.LogErrorFromException(ex, showStackTrace: true);
         return false;
      }
   }
}
