using System.Transactions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Contracts;
using Compze.Threading.ResourceAccess;
using static Compze.Contracts.Contract;

namespace Compze.Internals.SystemCE.TransactionsCE._internal;

///<summary>The durable resources that take part in each transaction through Compze's libraries: those that commit what the transaction<br/>
/// writes to where it outlives the process, such as a database connection or a state store's files. Each says so with<br/>
/// <see cref="TakesPart"/>, and a resource that can't share its transaction with another is refused, or refuses the other, as soon as<br/>
/// both are in it.</summary>
///<remarks>A transaction's durable resources commit one after the other, and none of Compze's can recover what a crash between two of<br/>
/// their commits leaves. A resource whose loss there would be more than a consumer expects says so with<br/>
/// <see cref="DurableResource.ThatCantShareItsTransaction"/>. A transaction's record is dropped when it completes.</remarks>
static class DurableResourcesOfTransactions
{
   static readonly IThreadShared<Dictionary<string, List<DurableResource>>> ByTransaction = IThreadShared.New(new Dictionary<string, List<DurableResource>>());

   ///<summary>Records that <paramref name="resource"/> takes part in <paramref name="transaction"/>. Throws, recording nothing, if it can't<br/>
   /// share the transaction with a resource already in it, or a resource already in it can't share it with <paramref name="resource"/>.</summary>
   internal static void TakesPart(Transaction transaction, DurableResource resource)
   {
      var transactionIdentifier = transaction.TransactionInformation.LocalIdentifier;
      var isTheFirstInTheTransaction = ByTransaction.Locked(byTransaction =>
      {
         var resources = byTransaction.GetOrAdd(transactionIdentifier, () => []);
         foreach(var alreadyIn in resources)
         {
            var whyNot = resource.WhyItCantShareItsTransactionWith(alreadyIn);
            State.Assert(whyNot == null, () => whyNot!);
         }

         resources.Add(resource);
         return resources.Count == 1;
      });

      if(isTheFirstInTheTransaction) transaction.TransactionCompleted += (_, _) => ByTransaction.Locked(it => it.Remove(transactionIdentifier));
   }
}
