using System.Threading;

namespace Compze.Testing.Threading;

class ThreadSnapshot
{
   public Thread Thread { get; } = Thread.CurrentThread;

   public TransactionSnapshot? Transaction { get; } = TransactionSnapshot.TakeSnapshot();
}