using Compze.Abstractions;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotsAndPublishCascades;

///<summary><see cref="ITaggregate.LoadFromSnapshot"/>: restoring a taggregate from a stored state, without its history, through the same tevent appliers that change it.</summary>
public static class Loading_a_taggregate_from_a_snapshot_specification
{
   public class Given_a_note_loaded_from_a_snapshot_of_one_tevent_at_version_1
   {
      readonly TaggregateId _id = new();
      readonly Note _note;
      readonly List<ITaggregateTevent> _teventsObserved = [];

      public Given_a_note_loaded_from_a_snapshot_of_one_tevent_at_version_1()
      {
         _note = Note.LoadFromSnapshot([new NoteTevent.Snapshot(_id, "restored")], version: 1);
         ((ITaggregate)_note).TeventStream.Subscribe(it => _teventsObserved.Add(it.Tevent));
      }

      [XF] public void the_tevent_applier_of_the_text_restored_it() => _note.Text.Must().Be("restored");
      [XF] public void the_snapshot_established_its_identity() => _note.Id.Must().Be(_id);
      [XF] public void its_version_is_1() => _note.Version.Must().Be(1);
      [XF] public void no_tevent_handler_was_called() => _note.TeventsHandled.Must().BeEmpty();
      [XF] public void no_publish_cascade_ended() => _note.PublishCascadesEnded.Must().BeEmpty();
      [XF] public void nothing_is_left_uncommitted() => ((ITaggregate)_note).Commit(tevents => tevents.Must().BeEmpty());
      [XF] public void loading_it_again_fails() => Invoking(() => ((ITaggregate)_note).LoadFromSnapshot([new NoteTevent.Snapshot(_id, "again")], 1)).Must().Throw<Exception>();

      public class when_it_is_edited : Given_a_note_loaded_from_a_snapshot_of_one_tevent_at_version_1
      {
         public when_it_is_edited() => _note.Edit("edited");

         [XF] public void the_text_is_the_edited_one() => _note.Text.Must().Be("edited");
         [XF] public void the_edit_has_version_2() => _note.Version.Must().Be(2);
         [XF] public void the_edit_carries_the_snapshots_identity() => _teventsObserved.Single().TaggregateId.Must().Be(_id);
         [XF] public void the_edit_is_the_only_tevent_to_commit() => ((ITaggregate)_note).Commit(tevents => tevents.Select(it => it.Tevent.GetType()).Must().SequenceEqual([typeof(NoteTevent.Edited)]));
      }
   }

   public class Given_a_note_loaded_from_a_snapshot_at_version_7
   {
      readonly Note _note = Note.LoadFromSnapshot([new NoteTevent.Snapshot(new TaggregateId(), "restored")], version: 7);

      [XF] public void its_version_is_7() => _note.Version.Must().Be(7);

      [XF] public void the_next_tevent_published_has_version_8()
      {
         _note.Edit("edited");
         _note.Version.Must().Be(8);
      }
   }

   public class Given_a_note_loaded_from_a_snapshot_with_two_senses
   {
      readonly Guid _firstSenseId = Guid.NewGuid();
      readonly Guid _secondSenseId = Guid.NewGuid();
      readonly Note _note;

      public Given_a_note_loaded_from_a_snapshot_with_two_senses() =>
         _note = Note.LoadFromSnapshot([
                                          new NoteTevent.Snapshot(new TaggregateId(), "restored"),
                                          new NoteTevent.Sense.Created(_firstSenseId, "first"),
                                          new NoteTevent.Sense.Created(_secondSenseId, "second")
                                       ],
                                       version: 1);

      [XF] public void the_entity_collection_holds_both_senses_in_order() => _note.Senses.InCreationOrder.Select(it => it.Id).Must().SequenceEqual([_firstSenseId, _secondSenseId]);
      [XF] public void each_sense_s_applier_restored_its_meaning() => _note.Senses.InCreationOrder.Select(it => it.Meaning).Must().SequenceEqual(["first", "second"]);
      [XF] public void the_version_is_the_snapshots_one_version() => _note.Version.Must().Be(1);

      [XF] public void a_sense_added_afterwards_joins_them()
      {
         _note.AddSense("third");
         _note.Senses.InCreationOrder.Select(it => it.Meaning).Must().SequenceEqual(["first", "second", "third"]);
      }
   }

   public class Given_an_order_loaded_from_a_snapshot_of_its_shipping_and_billing_addresses
   {
      readonly OrderTaggregate _order = OrderTaggregate.LoadFromSnapshot([
                                                                             new OrderTevent.Snapshot(new TaggregateId()),
                                                                             new ShippingAddressTevent<PostalAddressTevent.Changed>(new PostalAddressTevent.Changed("221B Baker Street")),
                                                                             new BillingAddressTevent<PostalAddressTevent.Changed>(new PostalAddressTevent.Changed("10 Downing Street"))
                                                                          ]);

      [XF] public void the_shipping_address_is_restored_through_its_slot() => _order.ShippingAddress.Street.Must().NotBeNull().Be("221B Baker Street");
      [XF] public void the_billing_address_is_restored_through_its_slot() => _order.BillingAddress.Street.Must().NotBeNull().Be("10 Downing Street");
   }

   public class Given_a_project_loaded_from_a_snapshot_of_its_checklist_of_two_shared_tentities
   {
      readonly ChecklistItemId _specifyId = ChecklistItemId.New();
      readonly ChecklistItemId _implementId = ChecklistItemId.New();
      readonly ProjectTaggregate _project;

      public Given_a_project_loaded_from_a_snapshot_of_its_checklist_of_two_shared_tentities() =>
         _project = ProjectTaggregate.LoadFromSnapshot([
                                                          new ProjectTevent.Snapshot(new TaggregateId()),
                                                          new ChecklistTevent<ChecklistItemTevent.Added>(new ChecklistItemTevent.Added(_specifyId, "Specify")),
                                                          new ChecklistTevent<ChecklistItemTevent.Added>(new ChecklistItemTevent.Added(_implementId, "Implement"))
                                                       ]);

      [XF] public void the_checklist_holds_both_items_in_order() => _project.Checklist.Entities.InCreationOrder.Select(it => it.Id).Must().SequenceEqual([_specifyId, _implementId]);
      [XF] public void each_item_s_applier_restored_its_title() => _project.Checklist.Entities.InCreationOrder.Select(it => it.Title).Must().SequenceEqual(["Specify", "Implement"]);
   }

   public class Given_snapshots_that_break_the_rules
   {
      [XF] public void a_snapshot_with_no_tevents_is_refused() =>
         Invoking(() => Note.LoadFromSnapshot([], 1)).Must().Throw<Exception>();

      [XF] public void a_snapshot_that_does_not_start_with_the_snapshot_marker_is_refused() =>
         Invoking(() => Note.LoadFromSnapshot([new NoteTevent.Created(new TaggregateId(), "created")], 1)).Must().Throw<Exception>();

      [XF] public void a_snapshot_with_the_snapshot_marker_on_a_later_tevent_is_refused() =>
         Invoking(() => Note.LoadFromSnapshot([new NoteTevent.Snapshot(new TaggregateId(), "first"), new NoteTevent.Snapshot(new TaggregateId(), "second")], 1)).Must().Throw<Exception>();

      [XF] public void a_snapshot_at_version_0_is_refused() =>
         Invoking(() => Note.LoadFromSnapshot([new NoteTevent.Snapshot(new TaggregateId(), "restored")], 0)).Must().Throw<Exception>();

      [XF] public void a_note_that_was_created_cannot_be_loaded_from_a_snapshot() =>
         Invoking(() => ((ITaggregate)Note.Create("created")).LoadFromSnapshot([new NoteTevent.Snapshot(new TaggregateId(), "restored")], 1)).Must().Throw<Exception>();
   }
}
