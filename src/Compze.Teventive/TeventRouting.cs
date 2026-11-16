using Compze.Contracts;
using System.Collections.Concurrent;
using System.Reflection;
using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive._private;
using Compze.Teventive.Taggregates.BaseClasses;

namespace Compze.Teventive;

///<summary>The tevent routing of one class: which applier, or which tevent handler, each tevent reaches in an instance of the class.<br/>
/// Declared once per class, kept in a static field, and shared by every instance, so an instance holds none of it.</summary>
///<remarks>A prototype (taggregate-experiments-opus): it replaces the per-instance <see cref="IMutableTeventDispatcher{TTevent}"/>s of<br/>
/// <see cref="Taggregates.BaseClasses.Taggregate{TTaggregate,TTaggregateTevent,TTaggregateTeventImplementation,TWrapperTeventInterface,TWrapperTeventImplementation}"/><br/>
/// and <see cref="TeventiveComponent{TParent,TParentTevent,TParentTeventImplementation,TComponent,TComponentTevent,TComponentTeventImplementation}"/><br/>
/// for the classes that declare one. See dev_docs/taggregate-improvements/opus on the taggregate-improvements branch.</remarks>
public abstract class TeventRouting
{
   ///<summary>Calls, in declaration order, every route of this class that matches <paramref name="wrapped"/>'s wrapper type, on <paramref name="instance"/>.</summary>
   internal abstract void Dispatch(object instance, IPublisherTevent<ITevent> wrapped);
}

///<summary>The tevent routing of the class <typeparamref name="TInstance"/>. See <see cref="TeventRouting"/>.</summary>
public sealed class TeventRouting<TInstance> : TeventRouting where TInstance : class
{
   readonly IReadOnlyList<TeventRoute> _routes;
   readonly bool _routesTeventHandlers;
   readonly IReadOnlyList<Type> _wrapperTypesThatMayGoUnhandled;

   ///<summary>The routes each wrapper tevent type matches, resolved the first time a tevent in a wrapper of that type is dispatched to any instance of the class.</summary>
   readonly ConcurrentDictionary<Type, TeventRoute[]> _routesByWrapperTeventType = new();

   TeventRouting(IReadOnlyList<TeventRoute> routes, bool routesTeventHandlers, IReadOnlyList<Type> wrapperTypesThatMayGoUnhandled)
   {
      _routes = routes;
      _routesTeventHandlers = routesTeventHandlers;
      _wrapperTypesThatMayGoUnhandled = wrapperTypesThatMayGoUnhandled;
   }

   ///<summary>Declares the tevent appliers of <typeparamref name="TInstance"/>: every tevent dispatched to an instance must reach at least one of them.</summary>
   public static TeventRouting<TInstance> DeclareAppliers(Action<Declaration> declare) => Declare(declare, routesTeventHandlers: false, inherited: []);

   ///<summary>Declares the tevent handlers of <typeparamref name="TInstance"/>: a tevent that reaches none of them is fine.</summary>
   public static TeventRouting<TInstance> DeclareHandlers(Action<Declaration> declare) => Declare(declare, routesTeventHandlers: true, inherited: []);

   ///<summary>The routing of <typeparamref name="TSubclass"/>: every route of this class, followed by those <paramref name="declare"/> adds.<br/>
   /// The added routes are typed on <typeparamref name="TSubclass"/>, so they reach its members without a cast.</summary>
   public TeventRouting<TSubclass> Extend<TSubclass>(Action<TeventRouting<TSubclass>.Declaration> declare) where TSubclass : class, TInstance =>
      TeventRouting<TSubclass>.Declare(declare, _routesTeventHandlers, inherited: _routes, inheritedMayGoUnhandled: _wrapperTypesThatMayGoUnhandled);

   ///<summary>A routing of the routes <see cref="ConventionTeventRouting"/> found by convention.</summary>
   internal static TeventRouting<TInstance> FromRoutes(IReadOnlyList<TeventRoute> routes, bool routesTeventHandlers) => new(routes, routesTeventHandlers, []);

   internal static TeventRouting<TInstance> Declare(Action<Declaration> declare, bool routesTeventHandlers, IReadOnlyList<TeventRoute> inherited, IReadOnlyList<Type>? inheritedMayGoUnhandled = null)
   {
      var declaration = new Declaration(routesTeventHandlers);
      declare(declaration);
      return new TeventRouting<TInstance>([..inherited, ..declaration.TeventRoutes], routesTeventHandlers, [..inheritedMayGoUnhandled ?? [], ..declaration.WrapperTypesThatMayGoUnhandled]);
   }

   internal override void Dispatch(object instance, IPublisherTevent<ITevent> wrapped)
   {
      var routes = _routesByWrapperTeventType.GetOrAdd(wrapped.GetType(), ResolveTeventRoutesFor);
      for(var index = 0; index < routes.Length; index++) routes[index].Deliver(instance, wrapped);
   }

   TeventRoute[] ResolveTeventRoutesFor(Type wrapperTeventType)
   {
      var matching = _routes.Where(route => route.Matches(wrapperTeventType)).ToArray();
      if(matching.Length == 0 && !MayGoUnhandled(wrapperTeventType)) throw new TeventUnhandledException(typeof(TInstance), wrapperTeventType);
      return matching;
   }

   bool MayGoUnhandled(Type wrapperTeventType) => _routesTeventHandlers || _wrapperTypesThatMayGoUnhandled.Any(it => it.IsAssignableFrom(wrapperTeventType));

   ///<summary>What a class's routing is declared through: its appliers or tevent handlers, and the routes to the components it owns.</summary>
   public sealed class Declaration
   {
      readonly bool _routesTeventHandlers;
      internal List<TeventRoute> TeventRoutes { get; } = [];
      internal List<Type> WrapperTypesThatMayGoUnhandled { get; } = [];

      internal Declaration(bool routesTeventHandlers) => _routesTeventHandlers = routesTeventHandlers;

      ///<summary>Calls <paramref name="handler"/> with the instance for every tevent assignable to <typeparamref name="TTevent"/>.<br/>
      /// The handler must hold no state of its own: declare it <see langword="static"/>, and reach the instance through its first parameter.</summary>
      public Declaration For<TTevent>(Action<TInstance, TTevent> handler) where TTevent : ITevent
      {
         AssertHoldsNoState(handler);
         TeventRoutes.Add(new TeventRoute(typeof(IPublisherTevent<TTevent>), (instance, wrapped) => handler((TInstance)instance, ((IPublisherTevent<TTevent>)wrapped).Tevent)));
         return this;
      }

      ///<summary>Routes every tevent of the component <paramref name="component"/> returns to that component's own appliers, or tevent handlers,<br/>
      /// as this routing routes. The component's tevent type is inferred from what <paramref name="component"/> returns.</summary>
      public Declaration Component<TComponentTevent>(Func<TInstance, ITeventRoutedMember<TComponentTevent>> component) where TComponentTevent : ITevent => Member(component);

      ///<summary>Routes every tevent of the entities in the collection <paramref name="entities"/> returns to that collection: a created tevent adds an<br/>
      /// entity, and every tevent reaches the entity whose id it carries.</summary>
      public Declaration Entities<TEntityTevent>(Func<TInstance, ITeventRoutedMember<TEntityTevent>> entities) where TEntityTevent : ITevent => Member(entities);

      ///<summary>Routes every tevent wrapped in the adopting wrapper tevent of the shared-tomponent slot <paramref name="slot"/> returns to that slot's tomponent.</summary>
      public Declaration SharedTomponent<TAdoptingWrapperTevent>(Func<TInstance, ITeventRoutedMember<TAdoptingWrapperTevent>> slot) where TAdoptingWrapperTevent : ITevent => Member(slot);

      Declaration Member<TMemberTevent>(Func<TInstance, ITeventRoutedMember<TMemberTevent>> member) where TMemberTevent : ITevent
      {
         AssertHoldsNoState(member);
         var toTeventHandlers = _routesTeventHandlers;
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
         TeventRoutes.Add(new TeventRoute(typeof(IPublisherTevent<TMemberTevent>), (instance, wrapped) => member((TInstance)instance).DispatchInternal(wrapped, toTeventHandlers)));
#pragma warning restore CS0618
         return this;
      }

      ///<summary>Lets tevents assignable to <typeparamref name="TTevent"/> reach no applier.</summary>
      public Declaration IgnoreUnhandled<TTevent>() where TTevent : ITevent
      {
         WrapperTypesThatMayGoUnhandled.Add(typeof(IPublisherTevent<TTevent>));
         return this;
      }

      static void AssertHoldsNoState(Delegate handler) =>
         Argument.Assert(handler.Target is null || handler.Target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0,
                         () => $"A handler in the tevent routing of {typeof(TInstance)} holds state: it captures this, or a local. The routing is shared by every instance, so a handler must reach the instance through its first parameter. Declare the lambda static.");
   }
}
