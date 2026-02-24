using Microsoft.Build.Framework;

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
#pragma warning disable CA1031 //Returning false IS surfacing an exception in this API
      catch(Exception ex)
      {
         Log.LogErrorFromException(ex, showStackTrace: true);
         return false;
      }
#pragma warning restore CA1031 //Returning false IS surfacing an exception in this API
   }
}
