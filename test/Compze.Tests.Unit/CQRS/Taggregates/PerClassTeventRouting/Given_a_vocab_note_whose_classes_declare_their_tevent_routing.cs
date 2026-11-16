using Compze.Contracts;
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
      [XF] public void the_forms_are_applied_by_the_components_own_routing() => _vocab.Forms.All.Must().SequenceEqual(["食べる", "たべる"]);
      [XF] public void the_components_applier_reads_a_member_only_the_vocab_has_through_its_typed_parent() => _vocab.Forms.Summary.Must().Be("taberu? 食べる,たべる");
      [XF] public void the_components_own_tevent_handler_published_the_count_of_the_forms() => _vocab.Forms.Count.Must().Be(2);
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

   public class after_it_is_created_and_archived_which_no_applier_handles_and_the_notes_routing_lets_go_unhandled : Given_a_vocab_note_whose_classes_declare_their_tevent_routing
   {
      readonly VocabNote _vocab = VocabNote.Create();

      public after_it_is_created_and_archived_which_no_applier_handles_and_the_notes_routing_lets_go_unhandled() => _vocab.Archive();

      [XF] public void the_archiving_is_published_without_an_applier() => _vocab.Version.Must().Be(2);
   }

   public class a_routing_declaration
   {
      [XF] public void with_a_handler_that_captures_a_local_throws() => Invoking(DeclareAHandlerThatCapturesALocal).Must().Throw<ArgumentAssertionFailedException>();
      [XF] public void with_handlers_that_capture_nothing_is_accepted() => DeclareHandlersThatCaptureNothing().Must().NotBeNull();

      static void DeclareAHandlerThatCapturesALocal()
      {
         var namesRenamedTo = new List<string>();
         TeventRouting<Note>.DeclareAppliers(it => it.For<INoteTevent.Renamed>((_, tevent) => namesRenamedTo.Add(tevent.Name)));
      }

      static TeventRouting<Note> DeclareHandlersThatCaptureNothing() => TeventRouting<Note>.DeclareAppliers(it => it.For<INoteTevent.Renamed>(static (_, _) => {}));
   }
}
