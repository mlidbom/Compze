// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive.Tevents.Public;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Teventive.Infrastructure.EventDispatching;

/// <summary>
/// Calls all matching handlers in the order they were registered when a tevent is Dispatched.<br/>
/// Handlers are registered through the <see cref="ITeventSubscriber{TTevent}"/> that <see cref="Register"/> returns; disposing that subscriber removes every subscription made through it.
/// </summary>
partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> : IMutableTeventDispatcher<TTevent>
   where TTevent : class, ITevent
{
   readonly List<RegisteredHandler> _handlers = [];
   readonly List<RegisteredHandler> _runBeforeHandlers = [];
   readonly List<RegisteredHandler> _runAfterHandlers = [];
   readonly bool _ignoreAllUnhandled;
   readonly IReadOnlySet<Type> _ignoredTevents;
   int _registrationVersion;
   Dictionary<Type, Action<ITevent>[]> _typeToHandlerCache = new();
   int _cachedRegistrationVersion;
   // ReSharper disable once StaticMemberInGenericType
   static readonly Action<ITevent>[] NullHandlerList = [];

   internal CallMatchingHandlersInRegistrationOrderTeventDispatcher(TeventDispatcherConfig config)
   {
      _ignoreAllUnhandled = config.Options.HasFlag(TeventDispatcherOptions.IgnoreAllUnhandled);
      //Routing operates exclusively on wrapper types, so an inner tevent type in the ignore configuration is translated: ignoring a tevent type ignores every wrapping of it.
      _ignoredTevents = config.IgnoredUnhandled.Select(PublisherIdentifyingTevent.WrapperTypeMatchingAllWrappingsOf).ToHashSet();
   }

   public ITeventSubscriber<TTevent> Register() => new TeventSubscriber(this);

   public void Dispatch(TTevent evt) => Dispatch(PublisherIdentifyingTevent.WrapTevent(evt));

   public void Dispatch(IPublisherIdentifyingTevent<TTevent> wrapped)
   {
      var handlers = GetHandlers(wrapped.GetType());
      for(var i = 0; i < handlers.Length; i++)
      {
         handlers[i](wrapped);
      }
   }

   public bool Handles(TTevent tevent) => GetHandlers(PublisherIdentifyingTevent.WrapperTypeFor(tevent.GetType()), validateHandlerExists: false).Any();

   Action<ITevent>[] GetHandlers(Type wrapperTeventType, bool validateHandlerExists = true)
   {
      if(_cachedRegistrationVersion != _registrationVersion)
      {
         _cachedRegistrationVersion = _registrationVersion;
         _typeToHandlerCache = new Dictionary<Type, Action<ITevent>[]>();
      }

      if(_typeToHandlerCache.TryGetValue(wrapperTeventType, out var arrayResult))
      {
         return arrayResult;
      }

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

      if(hasFoundHandler)
      {
         AddMatchingHandlersFrom(_runAfterHandlers);
      } else
      {
         if(validateHandlerExists && !MayGoUnhandled(wrapperTeventType))
         {
            throw new TeventUnhandledException(GetType(), wrapperTeventType);
         }

         return _typeToHandlerCache[wrapperTeventType] = NullHandlerList;
      }

      return _typeToHandlerCache[wrapperTeventType] = result.ToArray();

      void AddMatchingHandlersFrom(List<RegisteredHandler> beforeOrAfterHandlers)
      {
         foreach(var registeredHandler in beforeOrAfterHandlers)
         {
            var handler = registeredHandler.TryCreateHandlerFor(wrapperTeventType);
            if(handler != null) result.Add(handler);
         }
      }
   }

   bool MayGoUnhandled(Type wrapperTeventType) => _ignoreAllUnhandled || _ignoredTevents.Any(ignoredTeventType => ignoredTeventType.IsAssignableFrom(wrapperTeventType));
}
