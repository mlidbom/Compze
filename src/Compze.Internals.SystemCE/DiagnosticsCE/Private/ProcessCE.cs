using System.Diagnostics;

namespace Compze.Internals.SystemCE.DiagnosticsCE.Private;

static class ProcessCE
{
   extension(Process @this)
   {
      internal ProcessIdentity? Identity
      {
         get
         {
            try
            {
               return new ProcessIdentity(@this.Id, @this.StartTime);
            }
            catch(InvalidOperationException) //The process has exited.
            {
               return null;
            }
         }
      }
   }
}
