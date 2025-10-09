using System.Threading;

namespace Compze.Tests.Infrastructure.Threading;

public class ThreadSnapshot
{
   public Thread Thread { get; } = Thread.CurrentThread;

   public TransactionSnapshot? Transaction { get; } = TransactionSnapshot.TakeSnapshot();
}