using Compze.Contracts;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive._private.Infrastructure.EventDispatching;

partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   ///<summary>Every subscription registered with a dispatcher, in registration order, and the handlers each wrapper tevent type routes to:<br/>
   /// resolved the first time a tevent in a wrapper of that type is dispatched, and kept until the subscriptions next change.</summary>
   ///<remarks>Not safe for use from several threads: the dispatcher and its subscribers reach it only through the dispatcher's lock.</remarks>
   sealed class Subscriptions
   {
      // ReSharper disable once StaticMemberInGenericType One empty array for every dispatcher of the same tevent type is all that is needed.
      static readonly Action<ITevent>[] NoHandlers = [];

      readonly List<RegisteredHandler> _handlers = [];
      readonly List<RegisteredHandler> _runBeforeHandlers = [];
      readonly List<RegisteredHandler> _runAfterHandlers = [];
      readonly Dictionary<Type, Action<ITevent>[]> _handlersByWrapperTeventType = [];
      readonly bool _ignoresAllUnhandled;
      readonly IReadOnlySet<Type> _ignoredWrapperTeventTypes;

      internal Subscriptions(TeventDispatcherConfig config)
      {
         _ignoresAllUnhandled = config.IgnoresAllUnhandled;
         //Routing operates exclusively on wrapper types, so an inner tevent type in the ignore configuration is translated: ignoring a tevent type ignores every wrapping of it.
         _ignoredWrapperTeventTypes = config.IgnoredUnhandled.Select(PublisherTevent.WrapperTypeMatchingAllWrappingsOf).ToHashSet();
      }

      internal void AddHandler(RegisteredHandler subscription) => Add(subscription, _handlers);

      internal void AddRunBeforeHandlers(RegisteredHandler subscription) => Add(subscription, _runBeforeHandlers);

      internal void AddRunAfterHandlers(RegisteredHandler subscription) => Add(subscription, _runAfterHandlers);

      ///<summary>Removes <paramref name="subscriptions"/>, every one of which must have been added here.</summary>
      internal void Remove(IEnumerable<RegisteredHandler> subscriptions)
      {
         foreach(var subscription in subscriptions)
         {
            var wasRegistered = _handlers.Remove(subscription) || _runBeforeHandlers.Remove(subscription) || _runAfterHandlers.Remove(subscription);
            State.Assert(wasRegistered, () => "Only a subscription that was added can be removed.");
         }

         _handlersByWrapperTeventType.Clear();
      }

      ///<summary>The handlers to call, in order, for a tevent dispatched in a wrapper of <paramref name="wrapperTeventType"/>.<br/>
      /// Throws a <see cref="TeventUnhandledException"/> when no handler matches, unless the dispatcher's <see cref="TeventDispatcherConfig"/> lets such a tevent go unhandled.</summary>
      internal Action<ITevent>[] HandlersToCallFor(Type wrapperTeventType)
      {
         if(_handlersByWrapperTeventType.TryGetValue(wrapperTeventType, out var handlers)) return handlers;

         handlers = MatchingHandlersFor(wrapperTeventType);
         if(handlers.Length == 0 && !MayGoUnhandled(wrapperTeventType)) throw new TeventUnhandledException(typeof(CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent>), wrapperTeventType);

         return _handlersByWrapperTeventType[wrapperTeventType] = handlers;
      }

      ///<summary>Whether any handler matches a tevent dispatched in a wrapper of <paramref name="wrapperTeventType"/>.<br/>
      /// Unlike <see cref="HandlersToCallFor"/> it does not check whether the tevent may go unhandled, so it keeps what it resolves out of the cache:<br/>
      /// a cached empty list of handlers would let a later dispatch of a tevent that must not go unhandled pass without that check.</summary>
      internal bool HasHandlersFor(Type wrapperTeventType) =>
         (_handlersByWrapperTeventType.TryGetValue(wrapperTeventType, out var handlers) ? handlers : MatchingHandlersFor(wrapperTeventType)).Length > 0;

      void Add(RegisteredHandler subscription, List<RegisteredHandler> subscriptionsOfItsKind)
      {
         subscriptionsOfItsKind.Add(subscription);
         _handlersByWrapperTeventType.Clear();
      }

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
   }
}
