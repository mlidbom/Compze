namespace Compze.Sql.Common._internal;

///<summary>Thrown when one of Compze's database connections is opened, or first used, in a transaction that holds a durable resource<br/>
/// already, such as another database's connection or a consumer's own resource manager: a connection takes part in its transaction<br/>
/// as the transaction's one durable resource, so it can't take part in that one.</summary>
///<remarks>Why it replaces what .NET throws: .NET refuses a second durable resource by trying to make the transaction distributed and<br/>
/// failing, and its message, "Implicit distributed transactions have not been enabled", names neither resource. When the consumer's<br/>
/// resource takes part second instead, it is the consumer's call into .NET that throws, and Compze can't replace that message.</remarks>
class DurableResourceAlreadyInTheTransactionException(string connectionDescription, NotSupportedException refusal)
   : InvalidOperationException($"This transaction holds a durable resource already, such as another database's connection or a resource manager of your own, so {connectionDescription} can't take part in it. "
                            + "A database connection takes part in its transaction as the transaction's one durable resource, whose commit is the transaction's decision. "
                            + "A second durable resource would make the transaction distributed, which .NET refuses unless distributed transactions are enabled, and in which Compze's connections to SQLite, MySQL and PostgreSQL would refuse to prepare. "
                            + "Keep each database, and each other durable resource, in transactions of its own.",
                              refusal);
