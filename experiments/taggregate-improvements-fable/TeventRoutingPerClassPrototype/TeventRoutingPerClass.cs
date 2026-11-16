// A prototype of dev_docs/taggregate-improvements/fable/tevent-routing-per-class.md and a-taggregate-without-a-tevent-store.md:
// each teventive class declares its tevent appliers once, and an instance keeps a reference to that declaration and nothing else for routing.
// It is written to be measured and to try the declaration's shape in real C#, not to be Compze's code: it has no documentation comments
// to Compze's standard, a raw lock, and none of the validation the real base classes do.
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace TeventRoutingPerClass;

///<summary>What a parent hands a tevent to: a component, by the root tevent interface of the component's tevents.</summary>
public interface IApplyTevents<in TTevent>
{
   void ApplyTevent(TTevent tevent);
}

///<summary>What a child publishes through: its parent, which publishes tevents of its own implementation base class and so of every subclass of it.</summary>
public interface IPublishTevents<in TTeventImplementation>
{
   void PublishInternal(TTeventImplementation tevent);
}

///<summary>The tevent appliers, or the tevent handlers, that a teventive class has declared, in the order declared, and what each tevent type resolves to.<br/>
/// One instance for each class: every instance of the class routes through it.</summary>
public abstract class TeventAppliers
{
   static readonly Dictionary<Type, Action<object, ITevent>[]> NoTypeResolved = [];

   readonly List<Entry> _entries = [];
   readonly bool _routesByWrapper;
   bool _ignoresAllUnhandled;

   ///<summary>Never changed once published, so it is read without a lock: the technique of today's dispatcher.</summary>
   Dictionary<Type, Action<object, ITevent>[]> _resolvedByType = NoTypeResolved;

   ///<summary>For a root's declaration: the wrapper tevent implementation that the class publishes its tevents in.</summary>
   internal Type? WrapperTeventImplementation { get; }

   private protected TeventAppliers(bool routesByWrapper, Type? wrapperTeventImplementation, TeventAppliers? declarationOfTheBaseClass)
   {
      _routesByWrapper = routesByWrapper;
      WrapperTeventImplementation = wrapperTeventImplementation;
      if(declarationOfTheBaseClass == null) return;
      _entries.AddRange(declarationOfTheBaseClass._entries);
      _ignoresAllUnhandled = declarationOfTheBaseClass._ignoresAllUnhandled;
   }

   internal void Apply(object teventive, ITevent teventOrItsWrapper)
   {
      var type = teventOrItsWrapper.GetType();
      var appliers = Volatile.Read(ref _resolvedByType).TryGetValue(type, out var resolved) ? resolved : Resolve(type);
      for(var index = 0; index < appliers.Length; index++) appliers[index](teventive, teventOrItsWrapper);
   }

   Action<object, ITevent>[] Resolve(Type type)
   {
      lock(_entries)
      {
         if(_resolvedByType.TryGetValue(type, out var resolvedByAnotherThreadMeanwhile)) return resolvedByAnotherThreadMeanwhile;

         var appliers = _entries.Select(entry => entry.TryResolveFor(type, _routesByWrapper)).OfType<Action<object, ITevent>>().ToArray();
         if(appliers.Length == 0 && !_ignoresAllUnhandled) throw new InvalidOperationException($"No tevent applier is declared for {type}.");

         Volatile.Write(ref _resolvedByType, new Dictionary<Type, Action<object, ITevent>[]>(_resolvedByType) { [type] = appliers });
         return appliers;
      }
   }

   private protected void Add(Entry entry)
   {
      if(_resolvedByType.Count > 0) throw new InvalidOperationException("A declaration can't change once a tevent has been routed through it.");
      _entries.Add(entry);
   }

   private protected void LetEveryTeventGoUnhandled() => _ignoresAllUnhandled = true;

   private protected bool RoutesByWrapper => _routesByWrapper;

   private protected abstract class Entry
   {
      ///<summary>What the entry does with a tevent of <paramref name="type"/>, or null when it doesn't match. A root routes by the type of the wrapper, as today:<br/>
      /// a component and a shared tomponent are handed the tevent without a wrapper, and route by its own type.</summary>
      internal abstract Action<object, ITevent>? TryResolveFor(Type type, bool typeIsTheWrappers);
   }

   private protected sealed class ApplierEntry<TSelf, THandledTevent>(Action<TSelf, THandledTevent> applier) : Entry
      where TSelf : class
      where THandledTevent : ITevent
   {
      readonly Action<TSelf, THandledTevent> _applier = applier;

      internal override Action<object, ITevent>? TryResolveFor(Type type, bool typeIsTheWrappers)
      {
         if(typeIsTheWrappers)
            return type.IsAssignableTo(typeof(IPublisherTevent<THandledTevent>))
                      ? (self, wrapper) => _applier((TSelf)self, ((IPublisherTevent<THandledTevent>)wrapper).Tevent)
                      : null;

         return type.IsAssignableTo(typeof(THandledTevent))
                   ? (self, tevent) => _applier((TSelf)self, (THandledTevent)tevent)
                   : null;
      }
   }

   private protected sealed class ComponentEntry<TSelf, TComponentTevent>(Func<TSelf, IApplyTevents<TComponentTevent>> componentOf) : Entry
      where TSelf : class
      where TComponentTevent : ITevent
   {
      readonly Func<TSelf, IApplyTevents<TComponentTevent>> _componentOf = componentOf;

      internal override Action<object, ITevent>? TryResolveFor(Type type, bool typeIsTheWrappers)
      {
         if(typeIsTheWrappers)
            return type.IsAssignableTo(typeof(IPublisherTevent<TComponentTevent>))
                      ? (self, wrapper) => _componentOf((TSelf)self).ApplyTevent(((IPublisherTevent<TComponentTevent>)wrapper).Tevent)
                      : null;

         return type.IsAssignableTo(typeof(TComponentTevent))
                   ? (self, tevent) => _componentOf((TSelf)self).ApplyTevent((TComponentTevent)tevent)
                   : null;
      }
   }

   private protected sealed class SharedTomponentEntry<TSelf, TTomponentTevent, TAdoptingWrapperTevent>(Func<TSelf, SharedTomponent<TTomponentTevent>> tomponentOf) : Entry
      where TSelf : class
      where TTomponentTevent : class, ITevent
      where TAdoptingWrapperTevent : class, ITevent, IPublisherTevent<TTomponentTevent>
   {
      readonly Func<TSelf, SharedTomponent<TTomponentTevent>> _tomponentOf = tomponentOf;

      internal override Action<object, ITevent>? TryResolveFor(Type type, bool typeIsTheWrappers)
      {
         if(typeIsTheWrappers)
            return type.IsAssignableTo(typeof(IPublisherTevent<TAdoptingWrapperTevent>))
                      ? (self, wrapper) => _tomponentOf((TSelf)self).ApplyAdoptedTevent(((IPublisherTevent<TAdoptingWrapperTevent>)wrapper).Tevent.Tevent)
                      : null;

         return type.IsAssignableTo(typeof(TAdoptingWrapperTevent))
                   ? (self, adoptingWrapperTevent) => _tomponentOf((TSelf)self).ApplyAdoptedTevent(((TAdoptingWrapperTevent)adoptingWrapperTevent).Tevent)
                   : null;
      }
   }
}

///<summary>A class's declaration, as the class writes it: typed by the class itself and by the root interface of its tevents.</summary>
public sealed class TeventAppliers<TSelf, TTevent> : TeventAppliers
   where TSelf : class
   where TTevent : class, ITevent
{
   internal TeventAppliers(bool routesByWrapper, Type? wrapperTeventImplementation = null, TeventAppliers? declarationOfTheBaseClass = null)
      : base(routesByWrapper, wrapperTeventImplementation, declarationOfTheBaseClass) {}

   public TeventAppliers<TSelf, TTevent> For<THandledTevent>(Action<TSelf, THandledTevent> applier) where THandledTevent : TTevent
   {
      AssertCapturesNothing(applier);
      Add(new ApplierEntry<TSelf, THandledTevent>(applier));
      return this;
   }

   public TeventAppliers<TSelf, TTevent> Component<TComponentTevent>(Func<TSelf, IApplyTevents<TComponentTevent>> componentOf) where TComponentTevent : class, TTevent
   {
      AssertCapturesNothing(componentOf);
      Add(new ComponentEntry<TSelf, TComponentTevent>(componentOf));
      return this;
   }

   public TeventAppliers<TSelf, TTevent> SharedTomponent<TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>(
      Slot<TSelf, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> slot)
      where TTomponentTevent : class, ITevent
      where TAdoptingWrapperTevent : class, TTevent, IPublisherTevent<TTomponentTevent>
   {
      //Static fields are initialized in the order written: a slot declared below the declaration that enters it is null here.
      if(slot == null) throw new InvalidOperationException($"A slot entered in {typeof(TSelf).Name}'s declaration of tevent appliers is null: declare the slot above the declaration, in the same file.");
      Add(new SharedTomponentEntry<TSelf, TTomponentTevent, TAdoptingWrapperTevent>(slot.TomponentOf));
      return this;
   }

   public TeventAppliers<TSelf, TTevent> IgnoreAllUnhandled()
   {
      LetEveryTeventGoUnhandled();
      return this;
   }

   ///<summary>The start of a subclass's declaration: the entries of this, its base class's, come first, as a base class's constructor runs first today.</summary>
   public TeventAppliers<TSubclass, TTevent> ThenFor<TSubclass>(Type? wrapperTeventImplementationOfTheSubclass = null) where TSubclass : class, TSelf =>
      new(RoutesByWrapper, wrapperTeventImplementationOfTheSubclass ?? WrapperTeventImplementation, declarationOfTheBaseClass: this);

   ///<summary>Every instance of the class shares the lambda, so it must capture nothing: a lambda that captures has a closure object, with fields, as its target.</summary>
   static void AssertCapturesNothing(Delegate lambda)
   {
      var target = lambda.Target;
      if(target != null && target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0)
         throw new InvalidOperationException($"A lambda in {typeof(TSelf).Name}'s declaration captures something. Every instance of the class shares the declaration: take the instance as the lambda's first parameter.");
   }
}

///<summary>A taggregate whose class declares its tevent appliers once. What an instance keeps beyond its own state: the two declarations, its id and version,<br/>
/// and three small fields. Its lists exist only while it has tevents that are uncommitted, or not yet handed over.</summary>
public abstract class PerClassTaggregate<TTaggregate, TTevent, TTeventImplementation> : IPublishTevents<TTeventImplementation>
   where TTaggregate : PerClassTaggregate<TTaggregate, TTevent, TTeventImplementation>
   where TTevent : class, ITaggregateTevent
   where TTeventImplementation : TaggregateTevent, TTevent
{
   readonly TeventAppliers _appliers;
   readonly TeventAppliers? _handlers;

   List<ITaggregateTevent<TTevent>>? _uncommittedTevents;
   List<ITaggregateTevent<TTevent>>? _publishedTeventsNotYetHandedOver;
   int _reentrancyLevel;
   int _threadInPublish;
   bool _isApplyingTevents;
   bool _isHandingOverPublishedTevents;

   public TaggregateId Id { get; private set; } = null!;
   public int Version { get; private set; }

   protected PerClassTaggregate(TeventAppliers<TTaggregate, TTevent> appliers, TeventAppliers<TTaggregate, TTevent>? handlers = null)
   {
      _appliers = appliers;
      _handlers = handlers;
   }

   protected static TeventAppliers<TTaggregate, TTevent> DeclareTeventAppliers(Type wrapperTeventImplementation) => new(routesByWrapper: true, wrapperTeventImplementation);
   protected static TeventAppliers<TTaggregate, TTevent> DeclareTeventHandlers() => new TeventAppliers<TTaggregate, TTevent>(routesByWrapper: true).IgnoreAllUnhandled();

   protected static Slot<TTaggregate, TTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> DeclareSlot<TTomponentTevent, TAdoptingWrapperTevent>(
      Type adoptingWrapperTeventImplementation, Func<TTaggregate, SharedTomponent<TTomponentTevent>> tomponentOf)
      where TTomponentTevent : class, ITevent
      where TAdoptingWrapperTevent : class, TTevent, IPublisherTevent<TTomponentTevent> => new(adoptingWrapperTeventImplementation, tomponentOf);

   protected TPublishedTevent Publish<TPublishedTevent>(TPublishedTevent tevent) where TPublishedTevent : TTeventImplementation
   {
      if(_isApplyingTevents) throw new InvalidOperationException("You cannot publish tevents from within tevent appliers.");

      var thisCallIsTheThreadsOutermost = EnterPublishAssertingThatNoOtherThreadIsInIt();
      try
      {
         _reentrancyLevel++;
         try
         {
            var mutable = (IMutableTaggregateTevent)tevent;
            mutable.SetTaggregateVersionInternal(Version + 1);
            mutable.SetUtcTimeStampInternal(DateTime.UtcNow);
            if(Version == 0)
            {
               if(tevent is not ITaggregateCreatedTevent) throw new InvalidOperationException($"The first tevent a taggregate publishes must be an {nameof(ITaggregateCreatedTevent)}: {tevent.GetType()} isn't.");
            } else
            {
               mutable.SetTaggregateIdInternal(Id);
            }

            var wrapped = Wrap(tevent);
            Apply(tevent, wrapped);
            (_uncommittedTevents ??= []).Add(wrapped);
            (_publishedTeventsNotYetHandedOver ??= []).Add(wrapped);
            _handlers?.Apply(this, wrapped);
         }
         finally
         {
            _reentrancyLevel--;
         }

         if(_reentrancyLevel == 0)
         {
            AssertInvariantsAreMet();
            HandOverPublishedTevents();
         }
      }
      finally
      {
         if(thisCallIsTheThreadsOutermost) Volatile.Write(ref _threadInPublish, 0);
      }

      return tevent;
   }

   ///<summary>Rebuilds the taggregate's state from a compacted-history that its own code has constructed. Nothing is published, and nothing is left uncommitted.</summary>
   protected void LoadFromCompactedHistory(IEnumerable<TTeventImplementation> compactedHistory)
   {
      if(Version != 0) throw new InvalidOperationException("Only a taggregate at version 0 can load.");
      foreach(var tevent in compactedHistory)
      {
         var mutable = (IMutableTaggregateTevent)tevent;
         if(Version == 0)
         {
            if(tevent is not ITaggregateCreatedTevent) throw new InvalidOperationException($"A compacted-history starts with an {nameof(ITaggregateCreatedTevent)}: {tevent.GetType()} isn't.");
         } else
         {
            mutable.SetTaggregateIdInternal(Id);
         }

         mutable.SetTaggregateVersionInternal(Version + 1);
         Apply(tevent, Wrap(tevent));
      }

      AssertInvariantsAreMet();
   }

   ///<summary>Hands the uncommitted tevents to <paramref name="commitTevents"/>. They are taken out first: a tevent published from inside the callback is uncommitted for the next call.</summary>
   public void Commit(Action<IReadOnlyList<ITaggregateTevent<TTevent>>> commitTevents)
   {
      var uncommitted = _uncommittedTevents;
      _uncommittedTevents = null;
      commitTevents(uncommitted ?? (IReadOnlyList<ITaggregateTevent<TTevent>>)[]);
   }

   protected virtual void AssertInvariantsAreMet() {}

   ///<summary>Called when a call to Publish has completed: its tevent, and every tevent the tevent handlers published in response, are applied, and the invariants hold.<br/>
   /// Never called from inside itself: tevents published from inside a call are handed over when that call returns, in the order published.</summary>
   protected virtual void OnTeventsPublished(IReadOnlyList<ITaggregateTevent<TTevent>> publishedTevents) {}

   void HandOverPublishedTevents()
   {
      if(_isHandingOverPublishedTevents) return; //A hand-over is in progress further up this thread's stack: its loop hands these over, after those published before them.

      _isHandingOverPublishedTevents = true;
      try
      {
         while(_publishedTeventsNotYetHandedOver is { Count: > 0 } published)
         {
            _publishedTeventsNotYetHandedOver = null;
            OnTeventsPublished(published);
         }
      }
      finally
      {
         _isHandingOverPublishedTevents = false;
      }
   }

   ///<summary>One thread at a time may be in Publish. It is no thread affinity: the next call may come from any thread.</summary>
   bool EnterPublishAssertingThatNoOtherThreadIsInIt()
   {
      var thisThread = Environment.CurrentManagedThreadId;
      var threadInPublish = Interlocked.CompareExchange(ref _threadInPublish, thisThread, 0);
      if(threadInPublish == 0) return true;
      if(threadInPublish == thisThread) return false;
      throw new InvalidOperationException($"Thread {thisThread} called Publish on a {GetType().Name} while thread {threadInPublish} was in it. A taggregate is used by one thread at a time.");
   }

   void Apply(TTeventImplementation tevent, ITaggregateTevent<TTevent> wrapped)
   {
      _isApplyingTevents = true;
      try
      {
         if(tevent is ITaggregateCreatedTevent) Id = tevent.TaggregateId;
         Version = tevent.TaggregateVersion;
         _appliers.Apply(this, wrapped);
      }
      finally
      {
         _isApplyingTevents = false;
      }
   }

   ITaggregateTevent<TTevent> Wrap(TTeventImplementation tevent) => (ITaggregateTevent<TTevent>)TaggregateIdentifyingTevent.WrapIn(_appliers.WrapperTeventImplementation!, tevent);

   void IPublishTevents<TTeventImplementation>.PublishInternal(TTeventImplementation tevent) => Publish(tevent);
}

///<summary>A component whose class declares its tevent appliers once. An instance keeps its parent, to publish through, and its class's declaration.</summary>
public abstract class PerClassComponent<TComponent, TComponentTevent, TComponentTeventImplementation> : IApplyTevents<TComponentTevent>, IPublishTevents<TComponentTeventImplementation>
   where TComponent : PerClassComponent<TComponent, TComponentTevent, TComponentTeventImplementation>
   where TComponentTevent : class, ITaggregateTevent
   where TComponentTeventImplementation : TaggregateTevent, TComponentTevent
{
   readonly IPublishTevents<TComponentTeventImplementation> _parent;
   readonly TeventAppliers _appliers;

   protected PerClassComponent(IPublishTevents<TComponentTeventImplementation> parent, TeventAppliers<TComponent, TComponentTevent> appliers)
   {
      _parent = parent;
      _appliers = appliers;
   }

   protected static TeventAppliers<TComponent, TComponentTevent> DeclareTeventAppliers() => new(routesByWrapper: false);

   protected static Slot<TComponent, TComponentTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> DeclareSlot<TTomponentTevent, TAdoptingWrapperTevent>(
      Type adoptingWrapperTeventImplementation, Func<TComponent, SharedTomponent<TTomponentTevent>> tomponentOf)
      where TTomponentTevent : class, ITevent
      where TAdoptingWrapperTevent : class, TComponentTevent, IPublisherTevent<TTomponentTevent> => new(adoptingWrapperTeventImplementation, tomponentOf);

   protected void Publish(TComponentTeventImplementation tevent) => _parent.PublishInternal(tevent);

   void IApplyTevents<TComponentTevent>.ApplyTevent(TComponentTevent tevent) => _appliers.Apply(this, tevent);
   void IPublishTevents<TComponentTeventImplementation>.PublishInternal(TComponentTeventImplementation tevent) => Publish(tevent);
}

///<summary>What a slot does for the shared tomponent in it, whatever its owner's types are.</summary>
public interface ISlot<in TTomponentTevent>
{
   void Publish(object owner, TTomponentTevent tevent);
}

///<summary>A slot for a shared tomponent in an owner's class: a static of that class. It holds the adopting wrapper tevent's implementation, and how to get the shared<br/>
/// tomponent from an owner. It is the one place that wraps a tevent in the adopting wrapper tevent: when the shared tomponent publishes, and when the owner loads.</summary>
public sealed class Slot<TOwner, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> : ISlot<TTomponentTevent>
   where TOwner : class
   where TTomponentTevent : class, ITevent
   where TAdoptingWrapperTevent : class, IPublisherTevent<TTomponentTevent>
{
   readonly Type _adoptingWrapperTeventImplementation;
   readonly ConcurrentDictionary<Type, Func<object, object>> _constructorsByTeventType = new();

   internal Func<TOwner, SharedTomponent<TTomponentTevent>> TomponentOf { get; }

   internal Slot(Type adoptingWrapperTeventImplementation, Func<TOwner, SharedTomponent<TTomponentTevent>> tomponentOf)
   {
      _adoptingWrapperTeventImplementation = adoptingWrapperTeventImplementation.GetGenericTypeDefinition();
      TomponentOf = tomponentOf;
   }

   ///<summary>The slot and <paramref name="owner"/>, for the shared tomponent's constructor: a struct, so it allocates nothing.</summary>
   public SlotOfAnOwner<TTomponentTevent> In(TOwner owner) => new(owner, this);

   ///<summary>The owner's tevent that adopts <paramref name="tevent"/>: the slot's adopting wrapper tevent, closed over the tevent's own type.</summary>
   public TOwnerTeventImplementation Adopt(TTomponentTevent tevent) => (TOwnerTeventImplementation)_constructorsByTeventType.GetOrAdd(tevent.GetType(), CompileConstructorFor)(tevent);

   void ISlot<TTomponentTevent>.Publish(object owner, TTomponentTevent tevent) => ((IPublishTevents<TOwnerTeventImplementation>)owner).PublishInternal(Adopt(tevent));

   Func<object, object> CompileConstructorFor(Type teventType)
   {
      var closed = _adoptingWrapperTeventImplementation.MakeGenericType(teventType);
      var constructor = closed.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [teventType])
                     ?? throw new InvalidOperationException($"{closed} has no constructor that takes a {teventType}.");
      var tevent = Expression.Parameter(typeof(object));
      return Expression.Lambda<Func<object, object>>(Expression.New(constructor, Expression.Convert(tevent, teventType)), tevent).Compile();
   }
}

public readonly struct SlotOfAnOwner<TTomponentTevent>(object owner, ISlot<TTomponentTevent> slot)
{
   internal object Owner { get; } = owner;
   internal ISlot<TTomponentTevent> Slot { get; } = slot;
}

///<summary>A shared tomponent whose class declares its tevent appliers once. An instance keeps its owner and its slot, to publish through, and its class's declaration.</summary>
public abstract class SharedTomponent<TTomponentTevent> where TTomponentTevent : class, ITevent
{
   readonly object _owner;
   readonly ISlot<TTomponentTevent> _slot;
   readonly TeventAppliers _appliers;

   protected SharedTomponent(SlotOfAnOwner<TTomponentTevent> slot, TeventAppliers appliers)
   {
      _owner = slot.Owner;
      _slot = slot.Slot;
      _appliers = appliers;
   }

   ///<summary>The class itself is a type argument here: SharedTomponent has no type parameter for it.</summary>
   protected static TeventAppliers<TTomponent, TTomponentTevent> DeclareTeventAppliers<TTomponent>() where TTomponent : SharedTomponent<TTomponentTevent> => new(routesByWrapper: false);

   protected void Publish(TTomponentTevent tevent) => _slot.Publish(_owner, tevent);

   internal void ApplyAdoptedTevent(TTomponentTevent tevent) => _appliers.Apply(this, tevent);
}
