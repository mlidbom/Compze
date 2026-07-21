namespace Compze.Internals.SystemCE.TransactionsCE._private;

///<summary>Thrown by <see cref="TransactionCE.NoTransactionEscalationScope"/> when an operation escalates the ambient<br/>
/// transaction to a distributed (MSDTC) transaction while distributed transactions are not permitted.</summary>
///<remarks>Escalation happens when a second durable resource enlists in a transaction that began with one. In this codebase<br/>
/// every transaction uses exactly one connection (see <c>DbConnectionPool</c>), so an escalation is almost always an accident -<br/>
/// a bug to fix at the operation named in the message, not a runtime condition to handle. An application that genuinely needs<br/>
/// distributed transactions opts in with <see cref="TransactionCE.AllowDistributedTransactions"/>.</remarks>
class TransactionEscalatedToDistributedException(string scopeDescription)
   : InvalidOperationException($"{scopeDescription} escalated the ambient transaction to a distributed transaction. This almost "
                            + "always means a second durable resource enlisted in a transaction that must use exactly one connection. "
                            + $"If distributed transactions are genuinely intended, permit them at startup with {nameof(TransactionCE)}.{nameof(TransactionCE.AllowDistributedTransactions)}().");
