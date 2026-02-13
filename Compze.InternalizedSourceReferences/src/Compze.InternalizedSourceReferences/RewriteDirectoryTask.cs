using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Compze.InternalizedSourceReferences;

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
            $"Compze.InternalizedSourceReferences: Rewriting [{string.Join(", ", inputs.Select(i => $"'{i}'"))}] → '{OutputDirectory}'");

         SourceRewriter.RewriteDirectories(inputs, OutputDirectory);

         Log.LogMessage(MessageImportance.Normal,
            "Compze.InternalizedSourceReferences: Rewrite complete.");

         return true;
      }
      catch(Exception ex)
      {
         Log.LogErrorFromException(ex, showStackTrace: true);
         return false;
      }
   }
}
