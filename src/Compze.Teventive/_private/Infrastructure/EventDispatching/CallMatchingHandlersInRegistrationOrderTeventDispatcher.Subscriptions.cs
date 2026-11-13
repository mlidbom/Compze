using Compze.Contracts;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;

namespace Compze.Teventive._private.Infrastructure.EventDispatching;

partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   ///<summary>Every subscription registered with a dispatcher, in registration order, and the handlers each wrapper tevent type routes to:<br/>
   /// resolved the first time a tevent in a wrapper of that type is dispatched, and kept until the subscriptions next change.</summary>
   ///<remarks>Safe to use from several threads. Getting the handlers of a type already resolved, which is what nearly every dispatch does, takes no lock:<br/>
   /// the resolved handlers are a <see cref="Dictionary{TKey,TValue}"/> that is never changed once published. Resolving another type publishes a copy<br/>
   /// with that type added, and a change of subscriptions publishes an empty one. Everything else is done holding the lock <see cref="TakeLock"/> takes.</remarks>
   ///<remarks>The lock is a raw <see cref="Monitor"/> on this instance instead of a Compze.Threading primitive, which would allocate a lock object per dispatcher,<br/>
   /// and closures per use: see dev_docs/code_standard_exceptions/raw-monitor-in-the-tevent-dispatcher.md.</remarks>
   sealed class Subscriptions
   {
      // ReSharper disable StaticMemberInGenericType One of each for every dispatcher of the same tevent type is all that is needed.
      static readonly Dictionary<Type, Action<ITevent>[]> NoTypeResolved = [];
      static readonly Action<ITevent>[] NoHandlers = [];
      // ReSharper restore StaticMemberInGenericType

      readonly List<RegisteredHandler> _handlers = [];
      readonly List<RegisteredHandler> _runBeforeHandlers = [];
      readonly List<RegisteredHandler> _runAfterHandlers = [];
      readonly bool _ignoresAllUnhandled;
      readonly IReadOnlySet<Type> _ignoredWrapperTeventTypes;

      ///<summary>The handlers each wrapper tevent type routes to, for every type resolved since the subscriptions last changed. Never changed once published,<br/>
      /// so it is read without the lock; only <see cref="PublishResolvedHandlers"/> and <see cref="ForgetResolvedHandlers"/> replace it, holding the lock.</summary>
      Dictionary<Type, Action<ITevent>[]> _handlersByResolvedWrapperTeventType = NoTypeResolved;

      internal Subscriptions(TeventDispatcherConfig config)
      {
         _ignoresAllUnhandled = config.IgnoresAllUnhandled;
         //Routing operates exclusively on wrapper types, so an inner tevent type in the ignore configuration is translated: ignoring a tevent type ignores every wrapping of it.
         _ignoredWrapperTeventTypes = config.IgnoredUnhandled.Select(PublisherTevent.WrapperTypeMatchingAllWrappingsOf).ToHashSet();
      }

      ///<summary>The handlers to call, in order, for a tevent dispatched in a wrapper of <paramref name="wrapperTeventType"/>.<br/>
      /// Throws a <see cref="TeventUnhandledException"/> when no handler matches, unless the dispatcher's <see cref="TeventDispatcherConfig"/> lets such a tevent go unhandled.</summary>
      internal Action<ITevent>[] HandlersToCallFor(Type wrapperTeventType) =>
         Volatile.Read(ref _handlersByResolvedWrapperTeventType).TryGetValue(wrapperTeventType, out var handlers)
            ? handlers
            : ResolveHandlersToCallFor(wrapperTeventType);

      ///<summary>Whether any handler matches a tevent dispatched in a wrapper of <paramref name="wrapperTeventType"/>.<br/>
      /// Unlike <see cref="HandlersToCallFor"/> it does not check whether the tevent may go unhandled, so it publishes nothing it resolves:<br/>
      /// a published empty list of handlers would let a later dispatch of a tevent that must not go unhandled pass without that check.</summary>
      internal bool HasHandlersFor(Type wrapperTeventType)
      {
         if(Volatile.Read(ref _handlersByResolvedWrapperTeventType).TryGetValue(wrapperTeventType, out var handlers)) return handlers.Length > 0;

         using(TakeLock()) return MatchingHandlersFor(wrapperTeventType).Length > 0;
      }

      ///<summary>Takes the lock on this instance, until the returned <see cref="HeldLock"/> is disposed.</summary>
      ///<exception cref="TimeoutException">Another thread held the lock longer than <see cref="LockTimeout.Default"/>.</exception>
      internal HeldLock TakeLock()
      {
         //rationale: A raw Monitor instead of the Compze.Threading locking primitives, which allocate per dispatcher and per use:
         //   see dev_docs/code_standard_exceptions/raw-monitor-in-the-tevent-dispatcher.md.
#pragma warning disable CA2002 // Subscriptions is private to the dispatcher, so no code outside it can reach an instance to lock on it: the case CA2002's documentation names as safe to suppress.
         if(!Monitor.TryEnter(this, LockTimeout.Default))
#pragma warning restore CA2002
            throw new TimeoutException($"Timed out after {LockTimeout.Default.Value} waiting for the lock of a tevent dispatcher, which is held only while its subscriptions change or the handlers of a tevent type are resolved.");

         return new HeldLock(this);
      }

      ///<summary>Adds a subscription whose handler is called for every matching tevent. Only while holding the lock <see cref="TakeLock"/> takes.</summary>
      internal void AddHandler(RegisteredHandler subscription) => Add(subscription, _handlers);

      ///<summary>Adds a subscription whose handler is called before the handlers of every matching tevent that has any. Only while holding the lock <see cref="TakeLock"/> takes.</summary>
      internal void AddRunBeforeHandlers(RegisteredHandler subscription) => Add(subscription, _runBeforeHandlers);

      ///<summary>Adds a subscription whose handler is called after the handlers of every matching tevent that has any. Only while holding the lock <see cref="TakeLock"/> takes.</summary>
      internal void AddRunAfterHandlers(RegisteredHandler subscription) => Add(subscription, _runAfterHandlers);

      ///<summary>Removes <paramref name="subscriptions"/>, every one of which must have been added here. Only while holding the lock <see cref="TakeLock"/> takes.</summary>
      internal void Remove(IEnumerable<RegisteredHandler> subscriptions)
      {
         AssertTheLockIsHeld();
         foreach(var subscription in subscriptions)
         {
            var wasRegistered = _handlers.Remove(subscription) || _runBeforeHandlers.Remove(subscription) || _runAfterHandlers.Remove(subscription);
            State.Assert(wasRegistered, () => "Only a subscription that was added can be removed.");
         }

         ForgetResolvedHandlers();
      }

      void Add(RegisteredHandler subscription, List<RegisteredHandler> subscriptionsOfItsKind)
      {
         AssertTheLockIsHeld();
         subscriptionsOfItsKind.Add(subscription);
         ForgetResolvedHandlers();
      }

      Action<ITevent>[] ResolveHandlersToCallFor(Type wrapperTeventType)
      {
         using(TakeLock())
         {
            if(_handlersByResolvedWrapperTeventType.TryGetValue(wrapperTeventType, out var resolvedByAnotherThreadMeanwhile)) return resolvedByAnotherThreadMeanwhile;

            var handlers = MatchingHandlersFor(wrapperTeventType);
            if(handlers.Length == 0 && !MayGoUnhandled(wrapperTeventType)) throw new TeventUnhandledException(typeof(CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent>), wrapperTeventType);

            PublishResolvedHandlers(wrapperTeventType, handlers);
            return handlers;
         }
      }

      void PublishResolvedHandlers(Type wrapperTeventType, Action<ITevent>[] handlers)
      {
         var resolved = new Dictionary<Type, Action<ITevent>[]>(_handlersByResolvedWrapperTeventType.Count + 1);
         foreach(var (resolvedType, handlersOfResolvedType) in _handlersByResolvedWrapperTeventType) resolved.Add(resolvedType, handlersOfResolvedType);
         resolved.Add(wrapperTeventType, handlers);
         Volatile.Write(ref _handlersByResolvedWrapperTeventType, resolved);
      }

      void ForgetResolvedHandlers() => Volatile.Write(ref _handlersByResolvedWrapperTeventType, NoTypeResolved);

      Action<ITevent>[] MatchingHandlersFor(Type wrapperTeventType)
      {
         var result = new List<Action<ITevent>>();
         var hasFoundHandler = false;

         // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator LINQ would enumerate through the interface instead of List's struct enumerator in this performance-sensitive path.
         foreach(var registeredHandler in _handlers)
         {
            var handler = registeredHandler.TryCreateHandlerFor(wrapperTeventType);
            if(handler == null) continue;

            if(!hasFoundHandler)
            {
               AddMatchingHandlersFrom(_runBeforeHandlers);
               hasFoundHandler = true;
            }

            result.Add(handler);
         }

         if(!hasFoundHandler) return NoHandlers;

         AddMatchingHandlersFrom(_runAfterHandlers);
         return [..result];

         void AddMatchingHandlersFrom(List<RegisteredHandler> beforeOrAfterHandlers)
         {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator LINQ would enumerate through the interface instead of List's struct enumerator in this performance-sensitive path.
            foreach(var registeredHandler in beforeOrAfterHandlers)
            {
               var handler = registeredHandler.TryCreateHandlerFor(wrapperTeventType);
               if(handler != null) result.Add(handler);
            }
         }
      }

      bool MayGoUnhandled(Type wrapperTeventType) => _ignoresAllUnhandled || _ignoredWrapperTeventTypes.Any(ignoredTeventType => ignoredTeventType.IsAssignableFrom(wrapperTeventType));

      void AssertTheLockIsHeld() => State.Assert(Monitor.IsEntered(this), () => "The subscriptions of a tevent dispatcher may change only while holding its lock.");

      ///<summary>The lock on a <see cref="Subscriptions"/>, held until disposed. A <see langword="ref"/> <see langword="struct"/>, so holding it allocates nothing.</summary>
      internal readonly ref struct HeldLock(Subscriptions subscriptions)
      {
         readonly Subscriptions _subscriptions = subscriptions;

         public void Dispose() => Monitor.Exit(_subscriptions);
      }
   }
}
