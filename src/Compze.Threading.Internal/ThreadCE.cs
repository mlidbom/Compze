using System.Threading;

namespace Compze.Threading;

static class ThreadCE
{
   public static void InterruptAndJoin(this Thread @this)
   {
      @this.Interrupt();
      @this.Join();
   }
}
