// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Teventive.Infrastructure.EventDispatching;

/// <summary>
/// Calls all matching handlers in the order they were registered when an tevent is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> : IMutableTeventDispatcher<TTevent>
   where TTevent : class, ITevent
{
   readonly List<RegisteredHandler> _handlers = [];
   readonly List<Action<object>> _runBeforeHandlers = [];
   readonly List<Action<object>> _runAfterHandlers = [];
   readonly bool _ignoreAllUnhandled;
   readonly IReadOnlySet<Type> _ignoredTevents;
   int _totalHandlers;
   Dictionary<Type, Action<ITevent>[]> _typeToHandlerCache = new();
   int _cachedTotalHandlers;
   // ReSharper disable once StaticMemberInGenericType
   static readonly Action<ITevent>[] NullHandlerList = [];

   internal CallMatchingHandlersInRegistrationOrderTeventDispatcher(TeventDispatcherConfig config)
   {
      _ignoreAllUnhandled = config.Options.HasFlag(TeventDispatcherOptions.IgnoreAllUnhandled);
      _ignoredTevents = config.IgnoredUnhandled.SelectMany(TeventTypeAndItsWrapperTeventType).ToHashSet();
   }

   ///<summary>Dispatching wraps tevents in <see cref="IPublisherIdentifyingTevent{TTevent}"/> implementations, so ignoring a tevent type must also ignore its wrapped form.</summary>
   static Type[] TeventTypeAndItsWrapperTeventType(Type teventType) => [teventType, typeof(IPublisherIdentifyingTevent<>).MakeGenericType(teventType)];

   public ITeventHandlerRegistrar<TTevent> Register() => new RegistrationBuilder(this);


   //Urgent: Wrapping here seems arguable at best.
   public void Dispatch(TTevent evt) => Dispatch((IPublisherIdentifyingTevent<TTevent>)PublisherTypeIdentifyingTevent.WrapTevent(evt));

   public void Dispatch(IPublisherIdentifyingTevent<TTevent> wrapped)
   {
      var handlers = GetHandlers(wrapped.GetType());
      for(var i = 0; i < handlers.Length; i++)
      {
         handlers[i](wrapped);
      }
   }

   public bool Handles(TTevent tevent) => GetHandlers(tevent.GetType(), validateHandlerExists: false).Any();

   Action<ITevent>[] GetHandlers(Type type, bool validateHandlerExists = true)
   {
      if(_cachedTotalHandlers != _totalHandlers)
      {
         _cachedTotalHandlers = _totalHandlers;
         _typeToHandlerCache = new Dictionary<Type, Action<ITevent>[]>();
      }

      if(_typeToHandlerCache.TryGetValue(type, out var arrayResult))
      {
         return arrayResult;
      }

      var result = new List<Action<ITevent>>();
      var hasFoundHandler = false;

      // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator LINQ would enumerate through the interface instead of List's struct enumerator in this performance-sensitive path.
      foreach(var registeredHandler in _handlers)
      {
         var handler = registeredHandler.TryCreateHandlerFor(type);
         if(handler == null) continue;

         if(!hasFoundHandler)
         {
            result.AddRange(_runBeforeHandlers);
            hasFoundHandler = true;
         }

         result.Add(handler);
      }

      if(hasFoundHandler)
      {
         result.AddRange(_runAfterHandlers);
      } else
      {
         if(validateHandlerExists && !MayGoUnhandled(type))
         {
            throw new TeventUnhandledException(GetType(), type);
         }

         return _typeToHandlerCache[type] = NullHandlerList;
      }

      return _typeToHandlerCache[type] = result.ToArray();
   }

   bool MayGoUnhandled(Type teventType) => _ignoreAllUnhandled || _ignoredTevents.Any(ignoredTeventType => ignoredTeventType.IsAssignableFrom(teventType));
}
