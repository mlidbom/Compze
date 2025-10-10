using System.Threading;

namespace Compze.Utilities.Threading;

static class ThreadCE
{
   public static void InterruptAndJoin(this Thread @this)
   {
      @this.Interrupt();
      @this.Join();
   }
}
