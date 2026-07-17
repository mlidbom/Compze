using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Teventive.Tevents.Public;

namespace Compze.Tessaging.Engine;

///<summary>The one implementation of handler execution: one choreography executes the <see cref="TessageHandlerRoster"/>'s full<br/>
/// response to a tessage in one execution context. A tevent's response is every compatible handler, all within the one unit of<br/>
/// work — the tessage is the unit of handling, so its handlers commit or retry as a whole and partial handling is<br/>
/// unrepresentable; a tommand's is its single handler in a unit of work; a tuery's is its single handler in a scope. Every<br/>
/// arrival path calls this one executor — the local doors, an endpoint's inbox, an endpoint's transport server — and therefore<br/>
/// there is exactly one policy for a missing handler: the <see cref="NoHandlerException"/> the roster's lookups raise.</summary>
///<remarks>Each kind comes in two forms mirroring the unit-of-work model's UnitOfWork/Independent axis: one executing within the<br/>
/// context the caller already has (the resolver parameter says which), and one running the response in a context of its own<br/>
/// (<c>InOwnUnitOfWork</c>/<c>InIsolatedScope</c>). The handler-invoking forms are async — handlers are async-capable from<br/>
/// birth — while the own-context forms are synchronous for now because the unit-of-work envelope<br/>
/// (<c>ExecuteUnitOfWork</c>) is: they bridge inside, and go async with the doors when synchrony-follows-the-type reaches the<br/>
/// surfaces. Tevent observation is the deliberately transaction-ignoring watch surface; its current semantics — dispatched<br/>
/// inline at publish/arrival, undeterred by the triggering transaction's fate — are carried over unchanged until the<br/>
/// observation redesign (committed facts, off-thread).</remarks>
public class TessageHandlerExecutor
{
   readonly TessageHandlerRoster _roster;
   readonly IScopeFactory _scopeFactory;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;

   internal TessageHandlerExecutor(TessageHandlerRoster roster, IScopeFactory scopeFactory, IBackgroundExceptionReporter backgroundExceptionReporter)
   {
      _roster = roster;
      _scopeFactory = scopeFactory;
      _backgroundExceptionReporter = backgroundExceptionReporter;
   }

   ///<summary>Executes every participation handler whose subscription matches <paramref name="wrappedTevent"/>, within<br/>
   /// <paramref name="unitOfWork"/> — the publisher's own unit of work for a local publish, the inbox processing's own for an<br/>
   /// exactly-once arrival.</summary>
   public async Task ExecuteTeventHandlers(IPublisherTevent<ITevent> wrappedTevent, IUnitOfWorkResolver unitOfWork)
   {
      foreach(var handler in _roster.GetTeventHandlers(wrappedTevent.GetType()))
         await handler(wrappedTevent, unitOfWork).caf();
   }

   ///<summary>Executes the roster's full tevent response — see <see cref="ExecuteTeventHandlers"/> — as its own unit of work:<br/>
   /// the best-effort arrival form, where no caller context exists to join.</summary>
   public void ExecuteTeventHandlersInOwnUnitOfWork(IPublisherTevent<ITevent> wrappedTevent) =>
      _scopeFactory.ExecuteUnitOfWork(unitOfWork => ExecuteTeventHandlers(wrappedTevent, unitOfWork).GetAwaiter().GetResult());

   ///<summary>Dispatches <paramref name="wrappedTevent"/> to every matching observer: direct invocation, once, immediately, in a<br/>
   /// fresh scope with any ambient transaction suppressed. Invoked at every point a tevent is first registered — a local publish,<br/>
   /// an exactly-once tevent's inbox registration, a best-effort tevent's arrival.</summary>
   ///<remarks>The fresh scope keeps an observer's resolutions out of the triggering transaction: were observers handed the<br/>
   /// publisher's scope, a scoped database session they resolve could be enlisted in the very transaction observation exists to<br/>
   /// be undeterred by. A throwing observer is reported through the <see cref="IBackgroundExceptionReporter"/>, never retried —<br/>
   /// and never aborts the remaining observers or the publish/arrival that triggered the dispatch.</remarks>
   public void ExecuteTeventObservers(IPublisherTevent<ITevent> wrappedTevent)
   {
      var observers = _roster.GetTeventObservers(wrappedTevent.GetType());
      if(observers.Count == 0) return;

      //Outside any ambient transaction: observation is the rung that trades transactional coupling away — a locally published
      //tevent's observers hear it even if the publisher's transaction later rolls back, so they must not enlist in it.
      TransactionScopeCe.SuppressAmbient(() =>
      {
         using var scope = _scopeFactory.BeginScope();
         foreach(var observer in observers)
         {
            try
            {
               observer(wrappedTevent, scope.Resolver);
            }
#pragma warning disable CA1031 //A throwing observer is reported, never retried - and must not abort the remaining observers or the publish/arrival that triggered the dispatch.
            catch(Exception exception)
#pragma warning restore CA1031
            {
               _backgroundExceptionReporter.ReportException(exception);
            }
         }
      });
   }

   ///<summary>Whether any observer's subscription matches <paramref name="wrapperTeventType"/> — the deserialization-frugal<br/>
   /// question an arrival site asks from the envelope's type alone, so an arriving tessage nothing observes is never deserialized<br/>
   /// for observation's sake.</summary>
   public bool AnyTeventObserversFor(Type wrapperTeventType) => _roster.GetTeventObservers(wrapperTeventType).Count > 0;

   ///<summary>Executes the single handler for <paramref name="tommand"/> — a tommand whose type declares no result — within<br/>
   /// <paramref name="unitOfWork"/>: the caller's session for a strictly-local send, the inbox processing's own unit of work for<br/>
   /// an exactly-once arrival.</summary>
   public Task ExecuteTommandHandler(ITommand tommand, IUnitOfWorkResolver unitOfWork) =>
      _roster.GetVoidTommandHandler(tommand.GetType())(tommand, unitOfWork);

   ///<summary>Executes the single handler for <paramref name="tommand"/>, whose result answers the caller, within <paramref name="unitOfWork"/>.</summary>
   public async Task<TResult> ExecuteTommandHandler<TResult>(ITommand<TResult> tommand, IUnitOfWorkResolver unitOfWork) =>
      (TResult)await _roster.GetTommandHandlerWithResult(tommand.GetType())(tommand, unitOfWork).caf();

   ///<summary>Executes the single handler for <paramref name="tommand"/> — a tommand whose type declares no result — as its own<br/>
   /// unit of work: the remote-arrival form. The handler is resolved before the unit of work opens: a missing handler is a<br/>
   /// programming error surfacing as <see cref="NoHandlerException"/>, never work worth beginning a transaction for.</summary>
   public void ExecuteVoidTommandHandlerInOwnUnitOfWork(ITommand tommand)
   {
      var handler = _roster.GetVoidTommandHandler(tommand.GetType());
      _scopeFactory.ExecuteUnitOfWork(unitOfWork => handler(tommand, unitOfWork).GetAwaiter().GetResult());
   }

   ///<summary>Executes the single handler for <paramref name="tommand"/>, whose result answers the caller, as its own unit of<br/>
   /// work — see <see cref="ExecuteVoidTommandHandlerInOwnUnitOfWork"/>. Untyped because it serves the wire: the transport<br/>
   /// serializes whatever the handler returns.</summary>
   public object ExecuteTommandHandlerWithResultInOwnUnitOfWork(ITommand tommand)
   {
      var handler = _roster.GetTommandHandlerWithResult(tommand.GetType());
      return _scopeFactory.ExecuteUnitOfWork(unitOfWork => handler(tommand, unitOfWork).GetAwaiter().GetResult());
   }

   ///<summary>Executes the single handler for <paramref name="tuery"/> within <paramref name="scope"/> — the caller's session:<br/>
   /// a tuery changes nothing, so its execution needs a scope, not a unit of work, and reads join whatever consistency the<br/>
   /// caller's context has.</summary>
   public async Task<TResult> ExecuteTueryHandler<TResult>(ITuery<TResult> tuery, IScopeResolver scope) =>
      (TResult)await _roster.GetTueryHandler(tuery.GetType())(tuery, scope).caf();

   ///<summary>Executes the single handler for <paramref name="tuery"/> in a fresh transactionless scope of its own: the<br/>
   /// remote-arrival form. Untyped because it serves the wire — see <see cref="ExecuteTommandHandlerWithResultInOwnUnitOfWork"/>.</summary>
   public object ExecuteTueryHandlerInIsolatedScope(ITuery tuery)
   {
      var handler = _roster.GetTueryHandler(tuery.GetType());
      return _scopeFactory.ExecuteInIsolatedScope(scope => handler(tuery, scope).GetAwaiter().GetResult());
   }
}
