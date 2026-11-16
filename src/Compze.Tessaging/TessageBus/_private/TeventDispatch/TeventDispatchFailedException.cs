using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus._private.TeventDispatch;

///<summary>Dispatching a tevent failed when its unit of work committed: a participation handler, or the tevent's remote delivery<br/>
/// leg, threw. So the unit of work rolls back. It reaches the caller where the transaction completes, as the inner exception of a<br/>
/// <see cref="System.Transactions.TransactionAbortedException"/>, far from the code that published the tevent, so it names the<br/>
/// tevent, by the tevent's own description: a taggregate's tevent describes itself with its version and its taggregate.</summary>
class TeventDispatchFailedException(IPublisherTevent<ITevent> wrappedTevent, Exception exception)
   : Exception($"Dispatching {wrappedTevent.Tevent} failed when its unit of work committed, so the unit of work rolls back: {exception.Message}", exception);
