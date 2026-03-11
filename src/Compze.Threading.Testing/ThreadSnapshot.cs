namespace Compze.Threading.Testing;

///<summary>A snapshot of a thread's state captured when passing through an <see cref="IThreadGate"/>.</summary>
public class ThreadSnapshot
{
   ///<summary>The <see cref="TransactionSnapshot"/> of the thread at the time it passed through the gate, or null if no transaction was active.</summary>
   public TransactionSnapshot? Transaction { get; } = TransactionSnapshot.TakeSnapshot();
}
