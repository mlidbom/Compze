using Compze.Abstractions;
using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Teventive.Taggregates;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;
using Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>A taggregate of any shape can be restored from a snapshot: its entities and shared tentities too, which their collections add<br/>
/// without a tevent while the taggregate is being restored. And the check that a snapshot loses nothing sees those entities.</summary>
public static class Checking_that_taggregates_of_every_shape_lose_nothing_through_their_snapshots
{
   public class a_composite_taggregate_with_state_in_its_component_its_nested_component_and_entities_at_every_level
   {
      readonly CompositeTaggregate _taggregate = WithStateInEveryPart(new CompositeTaggregate("the root", new TaggregateId()));

      [XF] public void loses_nothing() => SnapshotRoundTrip.AssertLosesNothing(_taggregate, CompositeTaggregate.Empty, CompositeTaggregateSnapshot.HoldTheSameState);
   }

   public class a_composite_taggregate_whose_restore_leaves_out_its_entities
   {
      readonly CompositeTaggregate _taggregate = WithStateInEveryPart(CompositeTaggregate.CreateWhoseRestoreLeavesOutItsEntities("the root", new TaggregateId()));

      [XF] public void fails_the_check_naming_the_entity_collection_and_how_many_entities_each_holds() =>
         Invoking(() => SnapshotRoundTrip.AssertLosesNothing(_taggregate, CompositeTaggregate.EmptyWhoseRestoreLeavesOutItsEntities, CompositeTaggregateSnapshot.HoldTheSameState))
           .Must().Throw<Exception>()
           .Which.Message.Must().Contain("CompositeTaggregate._entities: 2 items in the original, 0 in the copy");
   }

   public class a_project_with_a_checklist_of_shared_tentities
   {
      readonly ProjectTaggregate _project = WithTwoChecklistItemsOneCompleted(ProjectTaggregate.Create());

      [XF] public void loses_nothing() => SnapshotRoundTrip.AssertLosesNothing(_project, ProjectTaggregate.Empty, ProjectSnapshot.HoldTheSameState);
   }

   public class a_project_whose_restore_leaves_out_its_checklist
   {
      readonly ProjectTaggregate _project = WithTwoChecklistItemsOneCompleted(ProjectTaggregate.CreateWhoseRestoreLeavesOutItsChecklist());

      [XF] public void fails_the_check_naming_the_checklist_and_how_many_items_each_holds() =>
         Invoking(() => SnapshotRoundTrip.AssertLosesNothing(_project, ProjectTaggregate.EmptyWhoseRestoreLeavesOutItsChecklist, ProjectSnapshot.HoldTheSameState))
           .Must().Throw<Exception>()
           .Which.Message.Must().Contain("ProjectTaggregate.Checklist: 2 items in the original, 0 in the copy");
   }

   public class a_cat_whose_class_extends_the_animals
   {
      readonly CatTaggregate _cat = CatTaggregate.RegisterBirth();

      [XF] public void loses_nothing() => SnapshotRoundTrip.AssertLosesNothing(_cat, CatTaggregate.Empty, AnimalSnapshot.HoldTheSameState);
   }

   public static class adding_a_restored_entity
   {
      [XF] public static void to_a_taggregate_created_by_publishing_throws() =>
         Invoking(() => new CompositeTaggregate("the root", new TaggregateId()).AddARestoredEntityOutsideARestore()).Must().Throw<StateAssertionFailedException>();

      [XF] public static void to_a_shared_tentity_collection_of_a_taggregate_created_by_publishing_throws() =>
         Invoking(() => ProjectTaggregate.Create().Checklist.AddRestored(ChecklistItemId.New())).Must().Throw<StateAssertionFailedException>();

      [XF] public static void to_a_restored_taggregate_after_its_first_publish_throws()
      {
         var project = ProjectTaggregate.Empty();
         project.RestoreFrom(ProjectTaggregate.Create().TakeSnapshot());
         project.AddChecklistItem("written after the restore");

         Invoking(() => project.Checklist.AddRestored(ChecklistItemId.New())).Must().Throw<StateAssertionFailedException>();
      }
   }

   static CompositeTaggregate WithStateInEveryPart(CompositeTaggregate taggregate)
   {
      taggregate.Component.Rename("the component");
      taggregate.Component.CComponent.Rename("the nested component");
      taggregate.Component.AddEntity("an entity of the component", Guid.NewGuid());
      taggregate.Component.AddEntity("another entity of the component", Guid.NewGuid()).Rename("another entity of the component, renamed");
      taggregate.Component.AddNonRemovableEntity("a non-removable entity of the component", Guid.NewGuid());
      taggregate.AddEntity("an entity at the root").AddEntity("a nested entity", Guid.NewGuid());
      taggregate.AddEntity("another entity at the root");
      return taggregate;
   }

   static ProjectTaggregate WithTwoChecklistItemsOneCompleted(ProjectTaggregate project)
   {
      project.AddChecklistItem("write the specification");
      project.AddChecklistItem("build it").Complete();
      return project;
   }
}
