using System.Threading;

namespace Compze.Utilities.Threading.Testing;

public class ThreadSnapshot
{
   public Thread Thread { get; } = Thread.CurrentThread;

   public TransactionSnapshot? Transaction { get; } = TransactionSnapshot.TakeSnapshot();
}
