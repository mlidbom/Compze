using System.Threading;

namespace Compze.SystemCE.ThreadingCE;

static class ThreadCE
{
   public static void InterruptAndJoin(this Thread @this)
   {
      @this.Interrupt();
      @this.Join();
   }
}