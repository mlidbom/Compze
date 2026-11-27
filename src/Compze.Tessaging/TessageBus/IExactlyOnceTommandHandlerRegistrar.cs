using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal registrar for exactly-once tommand handlers — what an endpoint-declaration's<br/>
/// <c>RegisterExactlyOnceTommandHandlers</c> override receives. It shows exactly one verb: registering the handler of an<br/>
/// <see cref="IExactlyOnceTommand"/>, the one tommand kind that is sent through the bus rather than navigated.<br/>
/// Implemented by <see cref="TessageBusHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface IExactlyOnceTommandHandlerRegistrar
{
   ///<summary>Registers the handler for <typeparamref name="TTommand"/>. The handler receives the <see cref="IUnitOfWorkResolver"/><br/>
   /// of the unit of work its execution IS — a tommand mutates state, so its effects commit or roll back as a whole.<br/>
   /// Async only, no synchronous form: exactly-once kinds are async end to end.</summary>
   ///<remarks>The handler also receives the token of the execution it serves: see<br/>
   /// <see cref="TessageBusHandlerRegistrar.ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task},HandlingOrder,Nullable{TimeSpan})"/>.</remarks>
   ///<param name="handler">The handler: the tommand, the unit of work, and the token of the execution it serves.</param>
   ///<param name="order">Whether the tommands keep their place in the order the endpoint's inbox handles tessages in, where the<br/>
   /// inbox handles them: those sent from another endpoint. See <see cref="HandlingOrder"/>.</param>
   ///<param name="transactionTimeout">How long the inbox's handling transaction may run before it times out and rolls back,<br/>
   /// which counts as a failed try: null keeps the default of 45 seconds. At most<br/>
   /// <see cref="System.Transactions.TransactionManager.MaximumTimeout"/>, 10 minutes unless the process raises it: a longer one<br/>
   /// explodes here. It applies where the inbox handles the tommands: one the endpoint itself sends runs in its sender's unit of<br/>
   /// work, within that one's timeout.</param>
   IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted, TimeSpan? transactionTimeout = null) where TTommand : IExactlyOnceTommand;

   ///<summary>The form of <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task},HandlingOrder,Nullable{TimeSpan})"/><br/>
   /// for a handler that waits on nothing it could give up.</summary>
   IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted, TimeSpan? transactionTimeout = null) where TTommand : IExactlyOnceTommand
      => ForTommand<TTommand>((tommand, unitOfWork, _) => handler(tommand, unitOfWork), order, transactionTimeout);
}
