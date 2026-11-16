using System.Collections.Concurrent;
using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection._internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageBus._internal;

namespace Compze.Tessaging.TessageBus._private.TeventDispatch;

///<summary>The <see cref="TransactionTeventQueue"/> of each transaction that publishes tevents in this container, so that every<br/>
/// unit of work in a transaction, a nested one that joined it too, publishes to the one queue of the transaction.</summary>
sealed class TransactionTeventQueues : ITransactionTeventDispatch
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<TransactionTeventQueues, ITransactionTeventDispatch>()
                                  .CreatedBy((TessageHandlerExecutor executor) => new TransactionTeventQueues(executor)));

   readonly TessageHandlerExecutor _executor;
   readonly ConcurrentDictionary<string, TransactionTeventQueue> _queuesByTransaction = new();

   TransactionTeventQueues(TessageHandlerExecutor executor) => _executor = executor;

   public void Enlist(ITeventDispatchParticipant participant) => OfTheAmbientTransaction().Enlist(participant);

   ///<summary>The queue of the ambient transaction, begun now if the transaction has none yet. It belongs to the unit of work that<br/>
   /// began the transaction, whose scope lives until the transaction completes, so that the tevents can be dispatched in it.</summary>
   internal TransactionTeventQueue OfTheAmbientTransaction()
   {
      var ambientTransaction = Transaction.Current;
      State.Assert(ambientTransaction != null,
                   () => "Tevents are dispatched when the transaction of their unit of work commits, and there is no ambient transaction. Run the work through ExecuteUnitOfWork.");
      var key = KeyOf(ambientTransaction);
      if(_queuesByTransaction.TryGetValue(key, out var existing)) return existing;

      var unitOfWork = AmbientUnitOfWork.Of(ambientTransaction);
      State.Assert(unitOfWork != null,
                   () => $"Tevents are published through {nameof(IUnitOfWorkTeventPublisher)} in a unit of work that ExecuteUnitOfWork began, and the ambient transaction was begun elsewhere, such as by a TransactionScope of its own. The tevents are dispatched when the transaction commits, in the scope of the unit of work that began it, so begin the transaction with ExecuteUnitOfWork.");
      //review: With two containers in one transaction, the unit of work that began it may belong to the other container, and
      //   the handlers of this container would resolve their services from that container's scope. TaggregateUnitsOfWork has the
      //   same limit, in its todo.
      var begun = new TransactionTeventQueue(_executor, unitOfWork);
      _queuesByTransaction[key] = begun;
      ambientTransaction.OnCompleted(() => _queuesByTransaction.TryRemove(key, out _));
      return begun;
   }

   static string KeyOf(Transaction transaction) => transaction.TransactionInformation.LocalIdentifier;
}
