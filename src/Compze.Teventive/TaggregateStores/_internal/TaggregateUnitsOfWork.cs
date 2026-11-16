using System.Collections.Concurrent;
using System.Transactions;
using Compze.DependencyInjection._internal;
using Compze.Contracts;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>The <see cref="TaggregateUnitOfWork"/> of each transaction that is changing taggregates of a container's stores, so that<br/>
/// the updater, references and indexes all find the ambient one from the ambient transaction alone.</summary>
sealed class TaggregateUnitsOfWork
{
   readonly ConcurrentDictionary<string, TaggregateUnitOfWork> _unitsOfWorkByTransaction = new();

   ///<summary>The taggregate unit of work of the ambient transaction, if it has begun one. Allocates nothing.</summary>
   internal TaggregateUnitOfWork? Ambient =>
      Transaction.Current is {} ambientTransaction ? _unitsOfWorkByTransaction.GetValueOrDefault(KeyOf(ambientTransaction)) : null;

   ///<summary>The taggregate unit of work of the ambient transaction, begun now if it has none yet. It belongs to the unit of work<br/>
   /// that began the transaction, whose scope lives until the transaction completes, so that the tevents can be dispatched in it.</summary>
   internal TaggregateUnitOfWork AmbientBeginningIt()
   {
      var ambientTransaction = Transaction.Current;
      State.Assert(ambientTransaction != null,
                   () => "Taggregates are changed inside a unit of work, and there is no ambient transaction. Run the work through ExecuteUnitOfWork.");
      var key = KeyOf(ambientTransaction);
      if(_unitsOfWorkByTransaction.TryGetValue(key, out var existing)) return existing;

      var unitOfWork = AmbientUnitOfWork.Of(ambientTransaction);
      State.Assert(unitOfWork != null,
                   () => "Taggregates are changed inside a unit of work that ExecuteUnitOfWork began, and the ambient transaction was begun elsewhere, such as by a TransactionScope of its own. The taggregates' tevents are dispatched when the transaction commits, in the scope of the unit of work that began it, so that unit of work must be the one ExecuteUnitOfWork runs.");
      //todo: With two containers in one transaction, the unit of work that began it may belong to the other container. Assert that
      //   it resolves this very instance, once there is a way to ask without resolving (jastudio-rep-opus, 2026-11-16).
      var begun = new TaggregateUnitOfWork(unitOfWork, ended: () => _unitsOfWorkByTransaction.TryRemove(key, out _));
      _unitsOfWorkByTransaction[key] = begun;
      return begun;
   }

   static string KeyOf(Transaction transaction) => transaction.TransactionInformation.LocalIdentifier;
}
