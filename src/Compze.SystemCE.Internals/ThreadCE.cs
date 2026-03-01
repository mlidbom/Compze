using System.Threading;

namespace Compze.Threading;

internal static class ThreadCE
{
   public static void InterruptAndJoin(this Thread @this)
   {
      @this.Interrupt();
      @this.Join();
   }
}
