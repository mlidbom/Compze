namespace Compze.Internals.SystemCE.TransactionsCE._internal;

///<summary>A durable resource taking part in a transaction, as <see cref="DurableResourcesOfTransactions"/> records it: what it is, and,<br/>
/// for a resource that can't share its transaction with another, why not.</summary>
sealed class DurableResource
{
   readonly Func<DurableResource, string>? _whyItCantShareItsTransactionWith;

   DurableResource(string description, Func<DurableResource, string>? whyItCantShareItsTransactionWith)
   {
      Description = description;
      _whyItCantShareItsTransactionWith = whyItCantShareItsTransactionWith;
   }

   ///<summary>A resource that may share its transaction with any other that allows it, such as a database connection: <paramref name="description"/><br/>
   /// is what it is, as a message names it, such as "a database connection".</summary>
   internal static DurableResource ThatCanShareItsTransaction(string description) => new(description, whyItCantShareItsTransactionWith: null);

   ///<summary>A resource that can't share its transaction with another durable resource: <paramref name="whyNot"/> gives the message that<br/>
   /// refuses the two, from the other.</summary>
   internal static DurableResource ThatCantShareItsTransaction(string description, Func<DurableResource, string> whyNot) => new(description, whyNot);

   ///<summary>What the resource is, as a message names it.</summary>
   internal string Description { get; }

   ///<summary>Why this resource and <paramref name="other"/> can't both take part in one transaction, or null if they can.</summary>
   internal string? WhyItCantShareItsTransactionWith(DurableResource other) => _whyItCantShareItsTransactionWith?.Invoke(other) ?? other._whyItCantShareItsTransactionWith?.Invoke(this);
}
