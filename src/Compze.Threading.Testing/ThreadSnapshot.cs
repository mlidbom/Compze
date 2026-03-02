namespace Compze.Threading.Testing;

public class ThreadSnapshot
{
   public TransactionSnapshot? Transaction { get; } = TransactionSnapshot.TakeSnapshot();
}
