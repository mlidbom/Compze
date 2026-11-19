using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;

public class Given_a_vocab_note_whose_classes_declare_their_tevent_routing
{
   public class after_it_is_created_renamed_and_given_two_forms : Given_a_vocab_note_whose_classes_declare_their_tevent_routing
   {
      readonly VocabNote _vocab;

      public after_it_is_created_renamed_and_given_two_forms()
      {
         _vocab = VocabNote.Create();
         _vocab.Rename("taberu");
         _vocab.Forms.Update("食べる,たべる");
      }

      [XF] public void the_name_is_applied_by_the_routing_the_vocab_inherits_from_the_note() => _vocab.Name.Must().Be("taberu");
      [XF] public void of_two_appliers_of_the_renaming_the_one_declared_later_ran_after_the_one_declared_before_it() => _vocab.NameAsTheLaterRenamedApplierFoundIt.Must().Be("taberu");
      [XF] public void the_vocabs_own_applier_of_the_renaming_ran_after_those_of_the_note_whose_routing_it_extends() => _vocab.QuestionAsTheVocabsRenamedApplierFoundIt.Must().Be("taberu?");
      [XF] public void the_forms_are_applied_by_the_components_own_routing() => _vocab.Forms.All.Must().SequenceEqual(["食べる", "たべる"]);
      [XF] public void the_components_applier_reads_a_member_only_the_vocab_has_through_its_typed_parent() => _vocab.Forms.Summary.Must().Be("taberu? 食べる,たべる");
      [XF] public void the_vocabs_tevent_handler_published_the_count_of_the_forms_through_the_component() => _vocab.Forms.Count.Must().Be(2);
      [XF] public void the_version_counts_the_created_renamed_updated_and_counted_tevents() => _vocab.Version.Must().Be(4);
      [XF] public void a_tevent_that_no_applier_handles_throws_TeventUnhandledException() => Invoking(() => _vocab.Forms.PublishATeventNoApplierHandles()).Must().Throw<TeventUnhandledException>();

      public class and_another_vocab_note_is_loaded_from_its_committed_history : after_it_is_created_renamed_and_given_two_forms
      {
         readonly VocabNote _loaded;

         public and_another_vocab_note_is_loaded_from_its_committed_history()
         {
            IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> history = [];
            ((ITaggregate)_vocab).Commit(committed => history = [..committed]);
            _loaded = VocabNote.LoadFromHistory(history);
         }

         [XF] public void the_loaded_note_has_the_same_name() => _loaded.Name.Must().Be("taberu");
         [XF] public void the_loaded_note_has_the_same_forms() => _loaded.Forms.All.Must().SequenceEqual(["食べる", "たべる"]);
         [XF] public void the_loaded_note_has_the_count_that_was_published_and_applies_it_without_running_the_handler_again() => _loaded.Forms.Count.Must().Be(2);
         [XF] public void the_loaded_note_has_the_same_version() => _loaded.Version.Must().Be(4);
      }

      public class and_then_given_two_senses_one_of_them_redefined_and_an_audio_file : after_it_is_created_renamed_and_given_two_forms
      {
         static readonly Guid EatSenseId = Guid.NewGuid();
         static readonly Guid ConsumeSenseId = Guid.NewGuid();

         public and_then_given_two_senses_one_of_them_redefined_and_an_audio_file()
         {
            _vocab.AddSense(EatSenseId, "to eat");
            _vocab.AddSense(ConsumeSenseId, "to use up").Redefine("to consume");
            _vocab.Audio.Add("taberu.mp3");
         }

         [XF] public void the_entity_collection_the_vocab_routes_to_added_both_senses() => _vocab.Senses.Select(it => it.Id).Must().SequenceEqual([EatSenseId, ConsumeSenseId]);
         [XF] public void each_sense_has_its_gloss_applied_by_its_own_routing() => _vocab.Senses.Select(it => it.Gloss).Must().SequenceEqual(["to eat", "to consume"]);
         [XF] public void the_shared_tomponent_has_the_audio_file_applied_by_its_own_routing() => _vocab.Audio.Files.Must().SequenceEqual(["taberu.mp3"]);
         [XF] public void the_version_counts_the_four_new_tevents() => _vocab.Version.Must().Be(8);

         public class and_another_vocab_note_is_loaded_from_all_of_its_committed_history : and_then_given_two_senses_one_of_them_redefined_and_an_audio_file
         {
            readonly VocabNote _loaded;

            public and_another_vocab_note_is_loaded_from_all_of_its_committed_history()
            {
               IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> history = [];
               ((ITaggregate)_vocab).Commit(committed => history = [..committed]);
               _loaded = VocabNote.LoadFromHistory(history);
            }

            [XF] public void the_loaded_note_has_both_senses_with_their_glosses() => _loaded.Senses.Select(it => $"{it.Id} {it.Gloss}").Must().SequenceEqual([$"{EatSenseId} to eat", $"{ConsumeSenseId} to consume"]);
            [XF] public void the_loaded_note_has_the_audio_file() => _loaded.Audio.Files.Must().SequenceEqual(["taberu.mp3"]);
            [XF] public void the_loaded_note_has_the_same_version() => _loaded.Version.Must().Be(8);
         }
      }
   }

   public class after_it_is_created_and_given_an_example_that_is_edited_then_removed : Given_a_vocab_note_whose_classes_declare_their_tevent_routing
   {
      static readonly Guid ExampleId = Guid.NewGuid();
      readonly VocabNote _vocab = VocabNote.Create();
      readonly VocabExample _example;

      public after_it_is_created_and_given_an_example_that_is_edited_then_removed()
      {
         _example = _vocab.AddExample(ExampleId);
         _example.Edit("I eat sushi.");
         _example.Remove();
      }

      [XF] public void the_example_has_the_id_of_its_created_tevent_which_its_routing_has_no_applier_of() => _example.Id.Must().Be(ExampleId);
      [XF] public void the_example_applied_its_edit() => _example.Text.Must().Be("I eat sushi.");
      [XF] public void the_examples_applier_of_its_removal_found_it_still_in_the_vocabs_examples() => _example.WasInTheVocabsExamplesWhenItAppliedItsRemoval.Must().BeTrue();
      [XF] public void the_example_is_no_longer_in_the_vocabs_examples() => _vocab.Examples.Must().BeEmpty();
   }

   public class after_it_is_created_and_archived_which_no_applier_handles_and_the_notes_routing_lets_go_unhandled : Given_a_vocab_note_whose_classes_declare_their_tevent_routing
   {
      readonly VocabNote _vocab = VocabNote.Create();

      public after_it_is_created_and_archived_which_no_applier_handles_and_the_notes_routing_lets_go_unhandled() => _vocab.Archive();

      [XF] public void the_archiving_is_published_without_an_applier() => _vocab.Version.Must().Be(2);
   }

   public class a_routing_declaration
   {
      [XF] public void with_a_handler_that_captures_a_local_throws() => Invoking(() => NoteDeclaringRoutings.DeclareAHandlerThatCapturesALocal()).Must().Throw<ArgumentAssertionFailedException>();
      [XF] public void with_handlers_that_capture_nothing_is_accepted() => NoteDeclaringRoutings.DeclareHandlersThatCaptureNothing().Must().NotBeNull();
      [XF] public void of_tevent_handlers_that_routes_to_a_component_throws() => Invoking(NoteDeclaringRoutings.DeclareTeventHandlersThatRouteToAComponent).Must().Throw<ArgumentAssertionFailedException>();

      ///<summary>A note class, since a routing is declared through the base class of the class it routes.</summary>
      abstract class NoteDeclaringRoutings : Note
      {
         internal static List<string> DeclareAHandlerThatCapturesALocal()
         {
            var namesRenamedTo = new List<string>();
            NoteAppliers.Extend<NoteDeclaringRoutings>(it => it.For<INoteTevent.Renamed>((_, tevent) => namesRenamedTo.Add(tevent.Name)));
            return namesRenamedTo;
         }

         internal static TeventRouting<NoteDeclaringRoutings, INoteTevent> DeclareHandlersThatCaptureNothing() => NoteAppliers.Extend<NoteDeclaringRoutings>(it => it.For<INoteTevent.Renamed>(static (_, _) => {}));

         internal static void DeclareTeventHandlersThatRouteToAComponent() => NoteHandlers.Extend<NoteDeclaringRoutings>(it => it.Component(static note => note.Forms));

         protected abstract FormsComponent Forms { get; }
      }
   }
}
