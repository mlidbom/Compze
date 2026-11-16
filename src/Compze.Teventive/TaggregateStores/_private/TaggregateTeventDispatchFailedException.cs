using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TaggregateStores._private;

///<summary>Dispatching a tevent of a taggregate failed when its unit of work committed, so the unit of work rolls back. It reaches the<br/>
/// caller where the transaction completes, as the inner exception of a <see cref="System.Transactions.TransactionAbortedException"/>,<br/>
/// far from the domain call that published the tevent, so it names the tevent and its taggregate.</summary>
class TaggregateTeventDispatchFailedException(ITaggregateTevent<ITaggregateTevent> tevent, Exception exception)
   : Exception($"Dispatching {tevent.Tevent.GetType().FullName}, version {tevent.Tevent.TaggregateVersion} of taggregate {tevent.Tevent.TaggregateId}, failed when its unit of work committed, so the unit of work rolls back: {exception.Message}", exception);
