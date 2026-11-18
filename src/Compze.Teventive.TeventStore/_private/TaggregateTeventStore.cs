using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive._internal;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore._private;

///<summary>The tevent store as one of a container's taggregate stores: it keeps each taggregate as its history, in the<br/>
/// <see cref="ITeventStore"/>, and builds an instance from that history whenever one is asked for. What one unit of work changes is<br/>
/// in its <see cref="TeventStoreSession"/>.</summary>
///<remarks>A deleted taggregate's history is kept, ending with its <see cref="ITaggregateDeletedTevent"/>, and is no taggregate: it is<br/>
/// neither got nor in <see cref="ReferencesToAll{TTaggregate}"/>.</remarks>
sealed class TaggregateTeventStore : ITaggregateStore
{
   readonly TaggregateUnitsOfWork _unitsOfWork;
   readonly IReadOnlyDictionary<Type, Type> _teventsByClass;

   internal TaggregateTeventStore(TeventStore teventStore, TaggregateUnitsOfWork unitsOfWork, IReadOnlyList<Type> taggregateClasses)
   {
      TeventStore = teventStore;
      _unitsOfWork = unitsOfWork;
      TaggregateClasses = taggregateClasses;
      _teventsByClass = taggregateClasses.ToDictionary(it => it, TeventsOf);
   }

   public string Name => "the tevent store";

   public IReadOnlyList<Type> TaggregateClasses { get; }

   internal TeventStore TeventStore { get; }

   ///<summary>Inside a unit of work that holds the taggregate, its instance. Otherwise one built from its history, read only.</summary>
   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
   {
      if(AmbientSession is {} session && session.TryGetHeld(id, out var held))
      {
         taggregate = held == null ? null : AsRequested<TTaggregate>(held);
         return taggregate != null;
      }

      taggregate = ReadOnlyInstanceFrom<TTaggregate>(TeventStore.GetTaggregateHistory(id));
      return taggregate != null;
   }

   public ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
      new TaggregateReference<TTaggregate>(this, id);

   ///<summary>The histories whose creation tevent is a tevent of the class, but for the deleted ones, read through the ambient<br/>
   /// transaction. The tevent store writes a unit of work's tevents at each publish, so a unit of work sees its own additions and<br/>
   /// deletions.</summary>
   public IReadOnlyCollection<ITaggregateReference<TTaggregate>> ReferencesToAll<TTaggregate>() where TTaggregate : class, ITaggregate
   {
      var deleted = TeventStore.IdsOfDeletedTaggregates();
      return [..TeventStore.StreamTaggregateIdsInCreationOrder(TeventsTellingApartTheHistoriesOf(ClassToBuildAs(typeof(TTaggregate))))
                           .Where(id => !deleted.Contains(id))
                           .Select(Reference<TTaggregate>)];
   }

   public ITaggregateStoreSession NewSession(TaggregateUnitOfWork unitOfWork) => new TeventStoreSession(this, unitOfWork);

   ///<summary>The taggregate <paramref name="history"/> is the history of, as a <typeparamref name="TTaggregate"/>, read only. Null if<br/>
   /// the history is empty or ends with a deletion, since the store then holds no such taggregate.</summary>
   TTaggregate? ReadOnlyInstanceFrom<TTaggregate>(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> history) where TTaggregate : class, ITaggregate
   {
      var taggregate = InstanceFrom<TTaggregate>(history);
      if(taggregate != null) ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
      return taggregate;
   }

   ///<summary>The taggregate <paramref name="history"/> is the history of, as a <typeparamref name="TTaggregate"/>, which may be changed.<br/>
   /// Null if the history is empty or ends with a deletion, since the store then holds no such taggregate.</summary>
   internal TTaggregate? InstanceFrom<TTaggregate>(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> history) where TTaggregate : class, ITaggregate =>
      history.None() || history[^1].Tevent is ITaggregateDeletedTevent ? null : Built<TTaggregate>(history);

   ///<summary>The taggregate as it was after the tevents of <paramref name="history"/>, a deletion included, read only: a version of<br/>
   /// it, for a reader of its history.</summary>
   internal TTaggregate ReadOnlyVersionFrom<TTaggregate>(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> history) where TTaggregate : class, ITaggregate
   {
      State.Assert(history.Any(), () => "A version of a taggregate is built from one tevent of its history at least.");
      var taggregate = Built<TTaggregate>(history);
      ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
      return taggregate;
   }

   TTaggregate Built<TTaggregate>(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> history) where TTaggregate : class, ITaggregate
   {
      var taggregate = (TTaggregate)Constructor.CreateInstance(ClassToBuildAs(typeof(TTaggregate)));
      //review: A history of another declared class, such as a vocab's id asked for as a sentence, fails here with the replay's
      //   InvalidCastException, which names neither class, where the memory image state store's message names both. Read, not run
      //   (2026-11-18): assert that the history's tevents are the class's, with a message naming both, as the documentation of
      //   ITaggregateStoreReader.Get promises a throw.
      taggregate.LoadFromHistory(history);
      return taggregate;
   }

   ///<summary>The class this store declares that a taggregate asked for as <paramref name="requestedClass"/> is: that class itself, or<br/>
   /// the one declared class that derives from it. A history doesn't say what class it is the history of.</summary>
   Type ClassToBuildAs(Type requestedClass)
   {
      var candidates = TaggregateClasses.Where(it => it.IsAssignableTo(requestedClass)).ToList();
      State.Assert(candidates.Count == 1,
                   () => candidates.Count == 0
                            ? $"The tevent store keeps no {requestedClass.FullName}: it keeps {string.Join(", ", TaggregateClasses.Select(it => it.FullName))}. Declare the class where the store is composed."
                            : $"The tevent store keeps several classes that are {requestedClass.FullName}: {string.Join(", ", candidates.Select(it => it.FullName))}. A history doesn't say which class it is the history of, so get the taggregate as its own class.");
      return candidates[0];
   }

   ///<summary>The tevents of <paramref name="taggregateClass"/>, which the creation tevent of each of its histories is. Asserts that no<br/>
   /// other declared class's tevents are such tevents too: a history doesn't say which class it is the history of.</summary>
   Type TeventsTellingApartTheHistoriesOf(Type taggregateClass)
   {
      var tevents = _teventsByClass[taggregateClass];
      var otherClassesWithSuchTevents = _teventsByClass.Where(it => it.Key != taggregateClass && it.Value.IsAssignableTo(tevents)).Select(it => it.Key.FullName).ToList();
      State.Assert(otherClassesWithSuchTevents.None(),
                   () => $"The tevent store can't tell the histories of {taggregateClass.FullName} from those of {string.Join(", ", otherClassesWithSuchTevents)}: their tevents are {tevents.FullName} too, and a history doesn't say which class it is the history of.");
      return tevents;
   }

   ///<summary>The tevent interface of <paramref name="taggregateClass"/>: the most derived of the tevent types of the<br/>
   /// <see cref="ITaggregate{TTevent}"/>s it implements.</summary>
   static Type TeventsOf(Type taggregateClass)
   {
      var teventTypes = taggregateClass.GetInterfaces()
                                       .Where(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ITaggregate<>))
                                       .Select(it => it.GetGenericArguments()[0])
                                       .ToList();
      return teventTypes.Single(candidate => teventTypes.All(candidate.IsAssignableTo));
   }

   internal static TTaggregate AsRequested<TTaggregate>(ITaggregate taggregate) where TTaggregate : class, ITaggregate
   {
      State.Assert(taggregate is TTaggregate, () => $"Taggregate {taggregate.Id} is a {taggregate.GetType().FullName}, not a {typeof(TTaggregate).FullName}.");
      return (TTaggregate)taggregate;
   }

   ///<summary>This store's session in the unit of work of the ambient transaction, if that unit of work has needed the store yet.</summary>
   TeventStoreSession? AmbientSession => (TeventStoreSession?)_unitsOfWork.Ambient?.ExistingSessionOf(this);
}
