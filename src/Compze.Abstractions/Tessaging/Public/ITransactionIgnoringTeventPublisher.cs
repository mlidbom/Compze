namespace Compze.Abstractions.Tessaging.Public;

///<summary>The publish escape hatch: publishes immediately and unconditionally — no on-commit deferral, so the tevent is emitted<br/>
/// even if the caller's transaction later rolls back (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). For<br/>
/// tracing/monitoring infrastructure that must emit <i>now</i>, regardless of the surrounding transaction's fate; everything else<br/>
/// publishes through <see cref="ITeventPublisher"/>, which honors the transaction.</summary>
///<remarks>A separate interface rather than a second method on <see cref="ITeventPublisher"/>, deliberately: depending on it makes<br/>
/// "this code emits out-of-band" visible in a constructor signature, and keeps the dangerous path off the common surface.<br/>
/// It rejects any tevent implementing <see cref="IMustBeSentTransactionally"/> (an <see cref="IExactlyOnceTevent"/>, most<br/>
/// commonly): immediate, unconditional delivery structurally cannot back a transactional send.</remarks>
public interface ITransactionIgnoringTeventPublisher
{
   ///<summary>Publishes <paramref name="tevent"/> immediately and unconditionally, with the ambient transaction suppressed:<br/>
   /// synchronously to this process's subscribed handlers and observers — detached from the caller's transaction, so their effects<br/>
   /// survive its rollback — and best-effort to remote subscribers right away, never deferred to commit.<br/>
   /// Throws for a tevent implementing <see cref="IMustBeSentTransactionally"/>.</summary>
   void Publish(ITevent tevent);
}
