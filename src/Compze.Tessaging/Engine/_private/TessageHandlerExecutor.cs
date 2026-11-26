using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine.Exceptions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Engine._private;

///<summary>The one implementation of handler execution: one choreography executes the <see cref="TessageHandlerRoster"/>'s full<br/>
/// response to a tessage in one execution context. A tevent's response is every compatible handler, all within the one unit of<br/>
/// work — the tessage is the unit of handling, so its handlers commit or retry as a whole and partial handling is<br/>
/// unrepresentable; a tommand's is its single handler in a unit of work; a tuery's is its single handler in a scope. Every<br/>
/// arrival path calls this one executor — the application-facing interfaces, an endpoint's inbox, an endpoint's transport server — and therefore<br/>
/// there is exactly one policy for a missing handler: the <see cref="NoHandlerException"/> the roster's lookups raise.</summary>
///<remarks>Each kind executes within the context the caller already has (the resolver parameter says which). Tevents and tueries<br/>
/// also come in a form running the response in a context of its own (<c>InOwnUnitOfWork</c>/<c>InIsolatedScope</c>), mirroring<br/>
/// the unit-of-work model's UnitOfWork/Independent axis; a remote tommand's own unit of work is opened by<br/>
/// <see cref="Typermedia._private.TypermediaHandlerExecutor"/>, whose retry must know whether the handler returned. Every form is async end to end — handlers are async-capable from birth,<br/>
/// and the own-context forms run the async unit-of-work envelope, whose ambient transaction flows across the handlers'<br/>
/// awaits. Tevent observation is not executed here: it is the deliberately transaction-ignoring watch surface, dispatched<br/>
/// off-thread by the engine's <see cref="TeventObservationDispatcher"/>.</remarks>
class TessageHandlerExecutor
{
   readonly TessageHandlerRoster _roster;
   readonly IScopeFactory _scopeFactory;

   internal TessageHandlerExecutor(TessageHandlerRoster roster, IScopeFactory scopeFactory)
   {
      _roster = roster;
      _scopeFactory = scopeFactory;
   }

   ///<summary>Executes every participation handler whose subscription matches <paramref name="wrappedTevent"/>, within<br/>
   /// <paramref name="unitOfWork"/> — the publisher's own unit of work for a local publish, the inbox processing's own for an<br/>
   /// exactly-once arrival.</summary>
   public async Task ExecuteTeventHandlersAsync(IPublisherTevent<ITevent> wrappedTevent, IUnitOfWorkResolver unitOfWork)
   {
      foreach(var handler in _roster.GetTeventHandlers(wrappedTevent.GetType()))
         await handler(wrappedTevent, unitOfWork).caf();
   }

   ///<summary>Executes the roster's full tevent response — see <see cref="ExecuteTeventHandlersAsync"/> — as its own unit of work:<br/>
   /// the best-effort arrival form, where no caller context exists to join.</summary>
   public async Task ExecuteTeventHandlersInOwnUnitOfWorkAsync(IPublisherTevent<ITevent> wrappedTevent) =>
      await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await ExecuteTeventHandlersAsync(wrappedTevent, unitOfWork).caf()).caf();

   ///<summary>Executes the single handler for <paramref name="tommand"/> — a tommand whose type declares no result — within<br/>
   /// <paramref name="unitOfWork"/>: the caller's session for a strictly-local send, the inbox processing's own unit of work for<br/>
   /// an exactly-once arrival.</summary>
   public Task ExecuteTommandHandlerAsync(ITommand tommand, IUnitOfWorkResolver unitOfWork) =>
      _roster.GetVoidTommandHandler(tommand.GetType())(tommand, unitOfWork);

   ///<summary>Executes the single handler for <paramref name="tommand"/>, whose result answers the caller, within <paramref name="unitOfWork"/>.</summary>
   public async Task<TResult> ExecuteTommandHandlerAsync<TResult>(ITommand<TResult> tommand, IUnitOfWorkResolver unitOfWork) =>
      (TResult)await ExecuteTommandHandlerWithResultAsync(tommand, unitOfWork).caf();

   ///<summary>Executes the single handler for <paramref name="tommand"/>, whose result answers the caller, within <paramref name="unitOfWork"/>.<br/>
   /// Untyped because it serves the wire: the transport serializes whatever the handler returns.</summary>
   public Task<object> ExecuteTommandHandlerWithResultAsync(ITommand tommand, IUnitOfWorkResolver unitOfWork) =>
      _roster.GetTommandHandlerWithResult(tommand.GetType())(tommand, unitOfWork);

   ///<summary>Executes the single handler for <paramref name="tuery"/> within <paramref name="scope"/> — the caller's session:<br/>
   /// a tuery changes nothing, so its execution needs a scope, not a unit of work, and reads join whatever consistency the<br/>
   /// caller's context has.</summary>
   public async Task<TResult> ExecuteTueryHandlerAsync<TResult>(ITuery<TResult> tuery, IScopeResolver scope) =>
      (TResult)await _roster.GetTueryHandler(tuery.GetType())(tuery, scope).caf();

   ///<summary>Executes the single handler for <paramref name="tuery"/> in a fresh transactionless scope of its own: the<br/>
   /// remote-arrival form. Untyped because it serves the wire — see <see cref="ExecuteTommandHandlerWithResultAsync"/>.</summary>
   public async Task<object> ExecuteTueryHandlerInIsolatedScopeAsync(ITuery tuery)
   {
      var handler = _roster.GetTueryHandler(tuery.GetType());
      return await _scopeFactory.ExecuteInIsolatedScopeAsync(async scope => await handler(tuery, scope).caf()).caf();
   }
}
