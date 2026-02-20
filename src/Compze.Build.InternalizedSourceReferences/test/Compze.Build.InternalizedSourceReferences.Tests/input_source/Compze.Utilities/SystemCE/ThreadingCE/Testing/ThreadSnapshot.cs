using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.Testing;

public class ThreadSnapshot
{
   public Thread Thread { get; } = Thread.CurrentThread;

   public TransactionSnapshot? Transaction { get; } = TransactionSnapshot.TakeSnapshot();
}
