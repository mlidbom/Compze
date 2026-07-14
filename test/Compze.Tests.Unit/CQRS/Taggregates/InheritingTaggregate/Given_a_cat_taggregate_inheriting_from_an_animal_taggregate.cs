using Compze.Abstractions.Tessaging.Public;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

///<summary>Publisher identification through taggregate inheritance: each level of the hierarchy wraps the tevents it publishes in its own wrapper<br/>
/// (<see cref="CatTaggregate"/> in <see cref="CatTevent{T}"/>, <see cref="DogTaggregate"/> in <see cref="DogTevent{T}"/>), so a subscriber can be<br/>
/// publisher-conscious - receiving, say, only the births cats publish - while publisher-indifferent subscribers receive every animal's births.</summary>
public class Given_a_cat_taggregate_inheriting_from_an_animal_taggregate
{
   public class after_a_cat_and_a_dog_register_their_births : Given_a_cat_taggregate_inheriting_from_an_animal_taggregate
   {
      readonly ITaggregateTevent<ITaggregateTevent> _catsWrappedBirthTevent;
      readonly ITaggregateTevent<ITaggregateTevent> _dogsWrappedBirthTevent;

      public after_a_cat_and_a_dog_register_their_births()
      {
         _catsWrappedBirthTevent = SingleCommittedTeventOf(CatTaggregate.RegisterBirth());
         _dogsWrappedBirthTevent = SingleCommittedTeventOf(DogTaggregate.RegisterBirth());
      }

      [XF] public void the_cats_birth_is_wrapped_in_the_cats_publisher_identifying_wrapper() => (_catsWrappedBirthTevent is ICatTevent<IAnimalTevent.Born>).Must().BeTrue();
      [XF] public void the_cats_wrapper_does_not_identify_a_dog_publisher() => (_catsWrappedBirthTevent is IDogTevent<IAnimalTevent.Born>).Must().BeFalse();
      [XF] public void the_dogs_birth_is_wrapped_in_the_dogs_publisher_identifying_wrapper() => (_dogsWrappedBirthTevent is IDogTevent<IAnimalTevent.Born>).Must().BeTrue();
      [XF] public void the_dogs_wrapper_does_not_identify_a_cat_publisher() => (_dogsWrappedBirthTevent is ICatTevent<IAnimalTevent.Born>).Must().BeFalse();
      [XF] public void both_wrappers_identify_an_animal_publisher() => (_catsWrappedBirthTevent is IAnimalTevent<IAnimalTevent.Born> && _dogsWrappedBirthTevent is IAnimalTevent<IAnimalTevent.Born>).Must().BeTrue();

      static ITaggregateTevent<ITaggregateTevent> SingleCommittedTeventOf(ITaggregate taggregate)
      {
         ITaggregateTevent<ITaggregateTevent>? committedWrappedTevent = null;
         taggregate.Commit(wrappedTevents => committedWrappedTevent = wrappedTevents.Single());
         return committedWrappedTevent!;
      }

      public class and_both_wrapped_tevents_are_dispatched_to_subscribers : after_a_cat_and_a_dog_register_their_births
      {
         readonly List<IAnimalTevent.Born> _receivedBySubscriberToCatWrappedBirths = [];
         readonly List<IAnimalTevent.Born> _receivedBySubscriberToAnimalWrappedBirths = [];
         readonly List<IAnimalTevent.Born> _receivedBySubscriberToTheInnerBirthTevent = [];

         public and_both_wrapped_tevents_are_dispatched_to_subscribers()
         {
            var dispatcher = IMutableTeventDispatcher<IAnimalTevent>.New();
            dispatcher.Register()
                      .ForWrapped<ICatTevent<IAnimalTevent.Born>>(wrappedTevent => _receivedBySubscriberToCatWrappedBirths.Add(wrappedTevent.Tevent))
                      .ForWrapped<IAnimalTevent<IAnimalTevent.Born>>(wrappedTevent => _receivedBySubscriberToAnimalWrappedBirths.Add(wrappedTevent.Tevent))
                      .For<IAnimalTevent.Born>(tevent => _receivedBySubscriberToTheInnerBirthTevent.Add(tevent));

            dispatcher.Dispatch((IPublisherTevent<IAnimalTevent>)_catsWrappedBirthTevent);
            dispatcher.Dispatch((IPublisherTevent<IAnimalTevent>)_dogsWrappedBirthTevent);
         }

         [XF] public void the_subscriber_to_cat_wrapped_births_receives_only_the_cats_birth() => _receivedBySubscriberToCatWrappedBirths.Single().Must().ReferenceEqual(_catsWrappedBirthTevent.Tevent);
         [XF] public void the_subscriber_to_animal_wrapped_births_receives_both_births() => _receivedBySubscriberToAnimalWrappedBirths.Must().HaveCount(2);
         [XF] public void the_subscriber_to_the_inner_birth_tevent_receives_both_births() => _receivedBySubscriberToTheInnerBirthTevent.Must().HaveCount(2);
      }
   }
}
