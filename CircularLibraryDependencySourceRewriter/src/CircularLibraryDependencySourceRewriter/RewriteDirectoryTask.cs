using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CircularLibraryDependencySourceRewriter;

public class RewriteDirectoryTask : Microsoft.Build.Utilities.Task
{
   [Required]
   public string InputDirectory { get; set; } = "";

   [Required]
   public string OutputDirectory { get; set; } = "";

   public override bool Execute()
   {
      try
      {
         Log.LogMessage(MessageImportance.Normal,
            $"CircularLibraryDependencySourceRewriter: Rewriting '{InputDirectory}' → '{OutputDirectory}'");

         SourceRewriter.RewriteDirectory(InputDirectory, OutputDirectory);

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
