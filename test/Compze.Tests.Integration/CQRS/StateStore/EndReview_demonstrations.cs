using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive;
using Compze.Teventive.StateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>Findings of the Opus end review of the taggregate-store spike, shown by running code against a state store. Each passing<br/>
/// specification shows the behaviour as built, which the review says is wrong or surprising.</summary>
public class EndReview_demonstrations : UniversalTestBase
{
   IDependencyInjectionContainer? _container;
   IDependencyInjectionContainer Container => _container!;

   void Compose()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.StateStore(store => store.Taggregate(() => ReviewCat.Empty())
                                                 .Taggregate(() => new StoredVocab())
                                                 .Index<StoredVocab, string>(vocab => [vocab.Question]));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _container = builder.Build();
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => Container.RootResolver.Resolve<ITaggregateStoreReader>();

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null) await _container.DisposeAsync();
   }

   ///<summary>Two references to one taggregate are equal only if they were asked for as the same class, or if the class asked for<br/>
   /// first was the more derived one: the store keeps the first reference made, and makes a new one for a class it can't be cast to.</summary>
   public class two_references_to_one_cat : EndReview_demonstrations
   {
      readonly TaggregateId _catId = new(Guid.NewGuid());

      public two_references_to_one_cat()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(ReviewCat.Born(_catId)));
      }

      [PCT] public void asked_for_as_an_animal_first_and_then_as_a_cat_are_not_equal()
      {
         ITaggregateReference<ReviewAnimal> asAnAnimal = Reader.Reference<ReviewAnimal>(_catId);
         ITaggregateReference<ReviewAnimal> asACat = Reader.Reference<ReviewCat>(_catId);

         asAnAnimal.Equals(asACat).Must().BeFalse();
         new HashSet<ITaggregateReference<ReviewAnimal>> { asAnAnimal, asACat }.Count.Must().Be(2);
      }

      [PCT] public void asked_for_as_a_cat_first_and_then_as_an_animal_are_the_same_object()
      {
         ITaggregateReference<ReviewAnimal> asACat = Reader.Reference<ReviewCat>(_catId);
         ITaggregateReference<ReviewAnimal> asAnAnimal = Reader.Reference<ReviewAnimal>(_catId);

         ReferenceEquals(asAnAnimal, asACat).Must().BeTrue();
      }
   }

   ///<summary>AssertCanBeChanged passes on a unit of work's copy, so a change made without a tevent is made. A copy that published nothing<br/>
   /// changes nothing at the commit, so the change is silently lost; had the unit of work published anything on it, it would have<br/>
   /// been saved, with no tevent saying so.</summary>
   public class a_change_made_without_a_tevent_on_a_copy_got_for_update : EndReview_demonstrations
   {
      readonly TaggregateId _vocabId = new(Guid.NewGuid());

      public a_change_made_without_a_tevent_on_a_copy_got_for_update()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(_vocabId, "taberu")));
      }

      [PCT] public void throws_nothing_and_is_lost_at_the_commit()
      {
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaningWithoutATevent("to eat"));

         Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("");
      }

      [PCT] public void is_saved_when_the_unit_of_work_also_publishes_anything_on_the_copy()
      {
         InUnitOfWork(taggregates =>
         {
            var copy = taggregates.GetForUpdate<StoredVocab>(_vocabId);
            copy.ChangeMeaningWithoutATevent("to eat");
            copy.ChangeQuestion("taberu");
         });

         Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("to eat");
      }
   }

   ///<summary>Outside a unit of work, Lookup returns the index's own array, typed as a read-only collection. Code that casts it back can<br/>
   /// write into the committed index that every other lookup reads.</summary>
   public class a_lookup_outside_a_unit_of_work : EndReview_demonstrations
   {
      readonly TaggregateId _vocabId = new(Guid.NewGuid());
      readonly TaggregateId _otherVocabId = new(Guid.NewGuid());

      public a_lookup_outside_a_unit_of_work()
      {
         Compose();
         InUnitOfWork(taggregates =>
         {
            taggregates.Save(StoredVocab.Create(_vocabId, "taberu"));
            taggregates.Save(StoredVocab.Create(_otherVocabId, "nomu"));
         });
      }

      ITaggregateIndex<StoredVocab, string> Index => Container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>();

      [PCT] public void returns_the_array_the_index_keeps_so_a_cast_can_change_what_later_lookups_find()
      {
         var found = Index.Lookup("taberu");
         ((ITaggregateReference<StoredVocab>[])found)[0] = Reader.Reference<StoredVocab>(_otherVocabId);

         Index.Lookup("taberu").Single().Id.Must().Be(_otherVocabId);
      }
   }
}

class ReviewAnimal : Taggregate<ReviewAnimal, IReviewAnimalTevent, ReviewAnimalTevent, IReviewAnimalTevent<IReviewAnimalTevent>, ReviewAnimalTevent<ReviewAnimalTevent>>, ITeventRouted
{
   protected static readonly TeventRouting<ReviewAnimal, IReviewAnimalTevent> AnimalAppliers = DeclareAppliers(it => it
     .For<IReviewAnimalTevent.Born>(static (animal, tevent) => animal.TimeOfBirth = tevent.UtcTimeStamp));

   TeventRouting ITeventRouted.TeventAppliers => AnimalAppliers;

   protected override Type WrapperTeventImplementation => typeof(ReviewAnimalTevent<ReviewAnimalTevent>);

   protected ReviewAnimal() {}

   protected DateTime TimeOfBirth { get; set; }

   protected void RegisterBirth(TaggregateId id) => Publish(new ReviewAnimalTevent.Born(id));
}

sealed class ReviewCat : ReviewAnimal, ITeventRouted, ISnapshotable<ReviewCat, ReviewCatSnapshot>
{
   static readonly TeventRouting<ReviewCat, IReviewAnimalTevent> CatAppliers = AnimalAppliers.Extend<ReviewCat>(_ => {});

   TeventRouting ITeventRouted.TeventAppliers => CatAppliers;

   ReviewCat() {}
   protected override Type WrapperTeventImplementation => typeof(ReviewCatTevent<ReviewCatTevent>);

   public static ReviewCat Born(TaggregateId id)
   {
      var cat = new ReviewCat();
      cat.RegisterBirth(id);
      return cat;
   }

   internal static ReviewCat Empty() => new();

   public ReviewCatSnapshot TakeSnapshot() => new(Id, TimeOfBirth);

   public void RestoreFrom(ReviewCatSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      TimeOfBirth = snapshot.TimeOfBirth;
   }
}

sealed class ReviewCatSnapshot(TaggregateId id, DateTime timeOfBirth)
{
   public TaggregateId Id { get; } = id;
   public DateTime TimeOfBirth { get; } = timeOfBirth;
}

interface IReviewAnimalTevent<out T> : ITaggregateTevent<T> where T : IReviewAnimalTevent;

interface IReviewAnimalTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested event interface follows semantic events naming convention
   interface Born : IReviewAnimalTevent, ITaggregateCreatedTevent;
#pragma warning restore CA1715
}

[UsedImplicitly] class ReviewAnimalTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IReviewAnimalTevent<T> where T : IReviewAnimalTevent;

class ReviewAnimalTevent : TaggregateTevent, IReviewAnimalTevent
{
   protected ReviewAnimalTevent() {}
   ReviewAnimalTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Born(TaggregateId taggregateId) : ReviewAnimalTevent(taggregateId), IReviewAnimalTevent.Born;
}

interface IReviewCatTevent : IReviewAnimalTevent;
interface IReviewCatTevent<out T> : IReviewAnimalTevent<T> where T : IReviewAnimalTevent;
[UsedImplicitly] class ReviewCatTevent<T>(T tevent) : ReviewAnimalTevent<T>(tevent), IReviewCatTevent<T> where T : IReviewAnimalTevent;
class ReviewCatTevent : ReviewAnimalTevent, IReviewCatTevent;
