using System.Collections.Concurrent;
using System.Transactions;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection._internal;

///<summary>The unit of work that began each ambient transaction, found from the transaction alone: so that code deep in a unit of<br/>
/// work, such as a taggregate store reached through a reference a caller holds, works in its scope without being handed it.</summary>
///<remarks>Only a unit of work that began its transaction is kept: one that joined an ambient transaction has a scope that ends<br/>
/// before the transaction commits, so work deferred to the commit can't run in it.</remarks>
static class AmbientUnitOfWork
{
   static readonly ConcurrentDictionary<string, IUnitOfWorkResolver> UnitsOfWorkByTransaction = new();

   ///<summary>The unit of work that began <paramref name="transaction"/>, if ExecuteUnitOfWork began it.</summary>
   internal static IUnitOfWorkResolver? Of(Transaction transaction) => UnitsOfWorkByTransaction.GetValueOrDefault(KeyOf(transaction));

   ///<summary>Records <paramref name="unitOfWork"/> as the one that began the ambient transaction, until the transaction completes.</summary>
   internal static void Began(IUnitOfWorkResolver unitOfWork)
   {
      var key = KeyOf(Transaction.Current!);
      UnitsOfWorkByTransaction[key] = unitOfWork;
      unitOfWork.OnCompleted(() => UnitsOfWorkByTransaction.TryRemove(key, out _));
   }

   static string KeyOf(Transaction transaction) => transaction.TransactionInformation.LocalIdentifier;
}
