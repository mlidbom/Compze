using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Registers an exactly-once endpoint's deferred tommand handlers: handlers that never run in their sender's unit of<br/>
/// work. The endpoint's inbox handles each tommand sent to one in a unit of work of its own, once the sender has committed,<br/>
/// whether this endpoint sent it or another did. Handed to the<br/>
/// <see cref="ExactlyOnceEndpointBuilder.RegisterDeferredTessageBusHandlers"/> callback and existing only inside it, exactly like<br/>
/// <see cref="TessageBusHandlerRegistrar"/>.</summary>
///<remarks>What it is for: work of several steps, each of which must be durable, such as a step that waits on something<br/>
/// outside the database. Each step's handler sends the next step to the endpoint itself, and each commits on its own: once a<br/>
/// step's sender has committed, the step is sure to be handled, across crashes and restarts, and the sender never waits for it<br/>
/// nor sees it fail. A tevent handler that takes part in a deferred handler's execution, by handling what it publishes, runs in<br/>
/// the deferred handler's unit of work, as in any sender's.</remarks>
///<remarks>A tommand the endpoint sends itself is admitted to the endpoint's own inbox in the sender's transaction: no outbox,<br/>
/// no transport. The endpoint handles what it sends itself exactly once, by default in the order the senders committed<br/>
/// (<see cref="HandlingOrder"/>), and tries a<br/>
/// handler again while it fails, for as long as the endpoint's <see cref="RetryPolicy"/> allows, as it does any tommand its<br/>
/// inbox handles. Sending one is refused before the endpoint has started and once its stop has ended. One sent while the<br/>
/// endpoint stops is handled by its next start.</remarks>
public sealed class DeferredTessageBusHandlerRegistrar : IDeferredTommandHandlerRegistrar
{
   //review-naming: DeferredTessageBusHandlerRegistrar, after Magnus's choice of Deferred (2026-11-27).
   readonly TessageHandlerRegistrations _registrations;
   bool _callbackHasEnded;

   internal DeferredTessageBusHandlerRegistrar(TessageHandlerRegistrations registrations) => _registrations = registrations;

   ///<summary>Registers the deferred handler for <typeparamref name="TTommand"/>: the endpoint's inbox runs it in a unit of work of<br/>
   /// its own, once the tommand's sender has committed. A tommand type has one handler, deferred or not: registering a second<br/>
   /// explodes. Async only, no synchronous form: exactly-once kinds are async end to end.</summary>
   ///<param name="handler">The handler: the tommand, the unit of work, and the token of the inbox's handling, cancelled when the<br/>
   /// endpoint's stop no longer waits for its inbox (<see cref="Endpoints.IEndpoint.StopAsync"/>). The handler's unit of work then<br/>
   /// rolls back, and the endpoint's next start handles the tommand again.</param>
   ///<param name="order">Whether the tommands keep their place in the order the endpoint's inbox handles tessages in. By default<br/>
   /// they do: the inbox handles them one at a time, in the order their senders committed. With <see cref="HandlingOrder.None"/>,<br/>
   /// it handles each as soon as it has capacity, at the same time as anything else: such as the independent steps of<br/>
   /// several flows, none of which should wait for another.</param>
   ///<param name="transactionTimeout">How long the inbox's handling transaction may run before it times out and rolls back,<br/>
   /// which counts as a failed try: null keeps the default of 45 seconds. A step that waits longer on something outside the<br/>
   /// database, such as a git worktree removal that takes a minute, needs a longer one. At most<br/>
   /// <see cref="System.Transactions.TransactionManager.MaximumTimeout"/>, 10 minutes unless the process raises it: a longer one<br/>
   /// explodes here. A long transaction holds its connection and its locks for as long as it runs: on SQLite, the one writer.</param>
   public DeferredTessageBusHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted, TimeSpan? transactionTimeout = null) where TTommand : IExactlyOnceTommand
   {
      //review-naming: the parameters order and transactionTimeout, as the design named them (2026-11-27), on every verb that takes them.
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddDeferredTommandHandler(handler, order, transactionTimeout);
      return this;
   }

   ///<summary>The form of <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task},HandlingOrder,Nullable{TimeSpan})"/><br/>
   /// for a handler that waits on nothing it could give up.</summary>
   public DeferredTessageBusHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted, TimeSpan? transactionTimeout = null) where TTommand : IExactlyOnceTommand
      => ForTommand<TTommand>((tommand, unitOfWork, _) => handler(tommand, unitOfWork), order, transactionTimeout);

   IDeferredTommandHandlerRegistrar IDeferredTommandHandlerRegistrar.ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order, TimeSpan? transactionTimeout) => ForTommand(handler, order, transactionTimeout);

   internal void EndCallback() => _callbackHasEnded = true;

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(DeferredTessageBusHandlerRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later. Declare every deferred handler inside the {nameof(ExactlyOnceEndpointBuilder.RegisterDeferredTessageBusHandlers)} callback.");
}
