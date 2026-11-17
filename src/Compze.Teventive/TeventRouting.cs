using Compze.Contracts;
using System.Collections.Concurrent;
using System.Reflection;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive._private;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive;

///<summary>The tevent routing of one class: which applier, or which tevent handler, each tevent reaches in an instance of the class.<br/>
/// Declared once per class, kept in a static field, and shared by every instance, so an instance holds none of it.</summary>
///<remarks>A prototype (taggregate-experiments-opus): it replaces the per-instance <see cref="IMutableTeventDispatcher{TTevent}"/>s of<br/>
/// <see cref="Taggregates.BaseClasses.Taggregate{TTaggregate,TTaggregateTevent,TTaggregateTeventImplementation,TWrapperTeventInterface,TWrapperTeventImplementation}"/><br/>
/// and <see cref="TeventiveComponent{TParent,TParentTevent,TParentTeventImplementation,TComponent,TComponentTevent,TComponentTeventImplementation}"/><br/>
/// for the classes that declare one. See dev_docs/taggregate-improvements/opus on the taggregate-improvements branch.<br/>
/// urgent: the order in which a tevent reaches the routes it matches is a guarantee Compze maintains (Magnus, 2026-11-17): one after<br/>
/// another, in the order declared, a base class's routes before a subclass's. Make sure every public description of routing states<br/>
/// it: the published docs under Compze.Teventive/Taggregates/_docs, which don't describe per-class routing yet (introduction.md<br/>
/// states the order for handlers registered per instance; step 3 of the spike rewrites reusable-components.md), and the doc comments<br/>
/// of <see cref="TeventRouting{TInstance}.DeclareAppliers"/>, <see cref="TeventRouting{TInstance}.DeclareHandlers"/>,<br/>
/// <see cref="TeventRouting{TInstance}.Extend{TSubclass}"/> and <see cref="TeventRouting{TInstance}.Declaration.For{TTevent}"/>.<br/>
/// <see cref="TeventRouting{TInstance}.Declaration"/>'s remarks state it, and<br/>
/// <c>Given_a_vocab_note_whose_classes_declare_their_tevent_routing</c> specifies it.</remarks>
public abstract class TeventRouting
{
   ///<summary>Calls, in declaration order, every route of this class that matches <paramref name="wrapped"/>'s wrapper type, on <paramref name="instance"/>.</summary>
   internal abstract void Dispatch(object instance, IPublisherTevent<ITevent> wrapped);

   ///<summary>The class this routing is declared for.</summary>
   private protected abstract Type DeclaredFor { get; }

   ///<summary>Asserts that the routing <paramref name="teventive"/> reads through <see cref="ITeventRouted"/>, if any, is declared for its<br/>
   /// exact class: a class that inherits a routing it doesn't declare for itself has forgotten its own, and would lose its appliers.</summary>
   internal static void AssertDeclaredForTheClassOf(ITeventRouted teventive)
   {
      AssertDeclaredFor(teventive.TeventAppliers);
      AssertDeclaredFor(teventive.TeventHandlers);

      void AssertDeclaredFor(TeventRouting? routing) =>
         State.Assert(routing == null || routing.DeclaredFor == teventive.GetType(),
                      () => $"{teventive.GetType().FullName} uses the tevent routing declared for {routing!.DeclaredFor.FullName}. Every class declares its own: extend its base class's with {nameof(TeventRouting<object>.Extend)}, and implement {nameof(ITeventRouted)} explicitly, listing it again in the class's base list.");
   }
}

///<summary>The tevent routing of the class <typeparamref name="TInstance"/>. See <see cref="TeventRouting"/>.</summary>
public sealed class TeventRouting<TInstance> : TeventRouting where TInstance : class
{
   readonly IReadOnlyList<TeventRoute> _routes;
   readonly bool _routesTeventHandlers;
   readonly IReadOnlyList<Type> _wrapperTypesThatMayGoUnhandled;

   ///<summary>The routes each wrapper tevent type matches, resolved the first time a tevent in a wrapper of that type is dispatched to any instance of the class.</summary>
   readonly ConcurrentDictionary<Type, TeventRoute[]> _routesByWrapperTeventType = new();

   private protected override Type DeclaredFor => typeof(TInstance);

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
   ///<remarks><typeparamref name="TSubclass"/> must derive directly from <typeparamref name="TInstance"/>, so that a chain of routings can't skip a<br/>
   /// class, and silently lose that class's appliers, components and slots.</remarks>
   public TeventRouting<TSubclass> Extend<TSubclass>(Action<TeventRouting<TSubclass>.Declaration> declare) where TSubclass : class, TInstance
   {
      Argument.Assert(typeof(TSubclass).BaseType == typeof(TInstance),
                      () => $"The tevent routing of {typeof(TSubclass).FullName} extends that of {typeof(TInstance).FullName}, which isn't its base class: {typeof(TSubclass).BaseType?.FullName} is. Extend the routing of the direct base class, so that no class's routes are skipped; every class declares its own.");
      return TeventRouting<TSubclass>.Declare(declare, _routesTeventHandlers, inherited: _routes, inheritedMayGoUnhandled: _wrapperTypesThatMayGoUnhandled);
   }

   static TeventRouting<TInstance> Declare(Action<Declaration> declare, bool routesTeventHandlers, IReadOnlyList<TeventRoute> inherited, IReadOnlyList<Type>? inheritedMayGoUnhandled = null)
   {
      var declaration = new Declaration(routesTeventHandlers);
      declare(declaration);
      return new TeventRouting<TInstance>([..inherited, ..declaration.TeventRoutes], routesTeventHandlers, [..inheritedMayGoUnhandled ?? [], ..declaration.WrapperTypesThatMayGoUnhandled]);
   }

   internal override void Dispatch(object instance, IPublisherTevent<ITevent> wrapped)
   {
      var wrapperTeventType = wrapped.GetType();
      if(!_routesByWrapperTeventType.TryGetValue(wrapperTeventType, out var routes)) routes = _routesByWrapperTeventType.GetOrAdd(wrapperTeventType, ResolveTeventRoutesFor); //TryGetValue first: passing the method group allocates a delegate on every call.
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
   ///<remarks>A tevent reaches every route it matches, one after another, in the order they were declared: a base class's routes first,<br/>
   /// as <see cref="TeventRouting{TInstance}.Extend{TSubclass}"/> adds a subclass's after them. So an applier may rely on those declared before<br/>
   /// it having applied the same tevent.</remarks>
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

      ///<summary>Routes every tevent wrapped in the adopting wrapper tevent of <paramref name="slot"/>, a slot that belongs to this class, to the<br/>
      /// shared tomponent in that slot of the instance. The instance keeps no slot object: the route gets the shared tomponent from it.</summary>
      public Declaration SharedTomponent<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>(
         SharedTomponentSlotOfAClass<TInstance, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> slot)
         where TOwnerTevent : class, ITaggregateTevent
         where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent
         where TTomponentTevent : class, ITevent
         where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>
      {
         // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract A slot declared below the routing in the same class is null here, whatever its type says.
         Argument.Assert(slot is not null, () => $"A slot entered in the tevent routing of {typeof(TInstance)} is null: static fields are initialized in the order written. Declare the slots in a nested static class, such as Slots, or above the routing.");
         if(_routesTeventHandlers) return this; //Shared tomponents have no tevent handlers.

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
         TeventRoutes.Add(new TeventRoute(typeof(IPublisherTevent<TAdoptingWrapperTevent>),
                                          (instance, wrapped) => slot.TomponentOf((TInstance)instance).ApplyAdoptedTeventInternal((IPublisherTevent<TTomponentTevent>)wrapped.Tevent)));
#pragma warning restore CS0618
         return this;
      }

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
