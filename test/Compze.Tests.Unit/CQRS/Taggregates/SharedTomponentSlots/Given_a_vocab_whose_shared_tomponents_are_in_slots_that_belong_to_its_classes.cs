using Compze.Must;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponentSlots;

///<summary>Slots for shared tomponents that belong to their owner's class, as static fields of a nested class, instead of a slot object in<br/>
/// each owner. The vocab has one such slot itself, and its component has two of one class, declared below the component's routing.</summary>
public class Given_a_vocab_whose_shared_tomponents_are_in_slots_that_belong_to_its_classes
{
   readonly Vocab _vocab = Vocab.Create("question");

   static string Words(WordSet words) => string.Join(", ", words.Words);

   public class after_a_word_is_added_to_the_synonyms_in_their_slot_of_the_component : Given_a_vocab_whose_shared_tomponents_are_in_slots_that_belong_to_its_classes
   {
      readonly List<ITaggregateTevent<ITaggregateTevent>> _committedTevents = [];

      public after_a_word_is_added_to_the_synonyms_in_their_slot_of_the_component()
      {
         _vocab.Related.Synonyms.Add("synonym");
         ((ITaggregate)_vocab).Commit(_committedTevents.AddRange);
      }

      [XF] public void the_synonyms_have_the_word() => Words(_vocab.Related.Synonyms).Must().Be("synonym");
      [XF] public void the_antonyms_in_the_other_slot_of_the_same_component_are_unaffected() => Words(_vocab.Related.Antonyms).Must().Be("");
      [XF] public void the_see_also_words_in_the_slot_of_the_vocab_itself_are_unaffected() => Words(_vocab.SeeAlso).Must().Be("");

      [XF] public void the_committed_tevent_is_the_vocabs_wrapping_of_the_synonyms_slots_adoption_of_the_addition() =>
         _committedTevents[^1].Must().Satisfy(it => it is IVocabTevent<ISynonymsTevent<IWordSetTevent.Added>>);

      [XF] public void the_committed_tevent_does_not_identify_the_antonyms_slot() =>
         _committedTevents[^1].Must().Satisfy(it => it is not IVocabTevent<IAntonymsTevent<IWordSetTevent.Added>>);

      public class and_words_are_added_to_the_antonyms_and_to_the_see_also_words : after_a_word_is_added_to_the_synonyms_in_their_slot_of_the_component
      {
         public and_words_are_added_to_the_antonyms_and_to_the_see_also_words()
         {
            _vocab.Related.Antonyms.Add("antonym");
            _vocab.SeeAlso.Add("see also");
            ((ITaggregate)_vocab).Commit(_committedTevents.AddRange);
         }

         [XF] public void each_shared_tomponent_has_only_its_own_word() =>
            $"{Words(_vocab.Related.Synonyms)} | {Words(_vocab.Related.Antonyms)} | {Words(_vocab.SeeAlso)}".Must().Be("synonym | antonym | see also");

         public class and_a_new_vocab_is_loaded_from_the_committed_history : and_words_are_added_to_the_antonyms_and_to_the_see_also_words
         {
            readonly Vocab _loaded;
            public and_a_new_vocab_is_loaded_from_the_committed_history() => _loaded = Vocab.LoadFromHistory(_committedTevents);

            [XF] public void each_of_its_shared_tomponents_has_its_own_word() =>
               $"{Words(_loaded.Related.Synonyms)} | {Words(_loaded.Related.Antonyms)} | {Words(_loaded.SeeAlso)}".Must().Be("synonym | antonym | see also");
         }

         public class and_the_committed_tevents_are_dispatched_to_subscribers : and_words_are_added_to_the_antonyms_and_to_the_see_also_words
         {
            readonly List<string> _receivedBySubscriberToTheVocabsOwnMeaning = [];
            readonly List<string> _receivedBySubscriberToTheSynonymsPublicationPath = [];
            readonly List<string> _receivedBySubscriberToTheSharedTomponentsOwnTeventType = [];

            public and_the_committed_tevents_are_dispatched_to_subscribers()
            {
               var dispatcher = IMutableTeventDispatcher<ITevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled);
               dispatcher.Register()
                         .For<IVocabTevent.ChangeToWhatItMatches>(tevent => _receivedBySubscriberToTheVocabsOwnMeaning.Add(tevent.GetType().Name))
                         .ForWrapped<IVocabTevent<ISynonymsTevent<IWordSetTevent.Added>>>(wrapped => _receivedBySubscriberToTheSynonymsPublicationPath.Add(wrapped.Tevent.Tevent.Word))
                         .For<IWordSetTevent.Added>(tevent => _receivedBySubscriberToTheSharedTomponentsOwnTeventType.Add(tevent.Word));
               foreach(var wrapped in _committedTevents) dispatcher.Dispatch(wrapped);
            }

            [XF] public void the_subscriber_to_the_meaning_only_the_synonyms_adopting_wrapper_tevent_has_receives_that_adopting_wrapper_tevent_alone() =>
               string.Join(", ", _receivedBySubscriberToTheVocabsOwnMeaning).Must().Be("SynonymsTevent`1");

            [XF] public void the_subscriber_to_the_synonyms_publication_path_receives_the_synonym() =>
               string.Join(", ", _receivedBySubscriberToTheSynonymsPublicationPath).Must().Be("synonym");

            [XF] public void the_subscriber_to_the_shared_tomponents_own_tevent_type_receives_nothing() =>
               _receivedBySubscriberToTheSharedTomponentsOwnTeventType.Must().BeEmpty();
         }
      }
   }
}
