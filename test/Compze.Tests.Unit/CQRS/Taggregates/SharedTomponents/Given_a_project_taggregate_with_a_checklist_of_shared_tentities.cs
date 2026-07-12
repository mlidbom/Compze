using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>Shared tentities through one taggregate: a project's checklist occupies ONE slot, so every <see cref="ChecklistItem"/> publishes through the same<br/>
/// adopting wrapper tevent (<see cref="IChecklistTevent{T}"/>), and the collection routes each tevent to the right item by the<br/>
/// <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> it carries - during live publishing and history replay alike.</summary>
public class Given_a_project_taggregate_with_a_checklist_of_shared_tentities
{
   readonly ProjectTaggregate _project = ProjectTaggregate.Create();

   public class after_two_checklist_items_are_added : Given_a_project_taggregate_with_a_checklist_of_shared_tentities
   {
      readonly ChecklistItem _specifyItem;
      readonly ChecklistItem _implementItem;

      public after_two_checklist_items_are_added()
      {
         _specifyItem = _project.AddChecklistItem("Specify");
         _implementItem = _project.AddChecklistItem("Implement");
      }

      [XF] public void the_checklist_contains_both_items_in_creation_order()
      {
         _project.Checklist.Entities.InCreationOrder[0].Must().ReferenceEqual(_specifyItem);
         _project.Checklist.Entities.InCreationOrder[1].Must().ReferenceEqual(_implementItem);
      }

      [XF] public void each_item_applies_its_own_added_tevent()
      {
         _specifyItem.Title.Must().NotBeNull().Be("Specify");
         _implementItem.Title.Must().NotBeNull().Be("Implement");
      }

      [XF] public void the_items_have_distinct_ids() => _specifyItem.Id.Equals(_implementItem.Id).Must().BeFalse();

      public class and_the_first_item_is_completed : after_two_checklist_items_are_added
      {
         public and_the_first_item_is_completed() => _specifyItem.Complete();

         [XF] public void the_completed_item_is_completed() => _specifyItem.IsCompleted.Must().BeTrue();
         [XF] public void the_other_item_is_not_completed() => _implementItem.IsCompleted.Must().BeFalse();

         [XF] public void the_committed_tevent_is_the_projects_wrapping_of_the_checklists_adoption_of_the_completion()
         {
            ITaggregateIdentifyingTevent<ITaggregateTevent>? lastCommittedTevent = null;
            ((ITaggregate)_project).Commit(committedTevents => lastCommittedTevent = committedTevents[^1]);
            (lastCommittedTevent is IProjectTevent<IChecklistTevent<IChecklistItemTevent.Completed>>).Must().BeTrue();
         }

         public class and_a_new_project_instance_is_loaded_from_the_committed_history : and_the_first_item_is_completed
         {
            readonly ProjectTaggregate _reloadedProject;

            public and_a_new_project_instance_is_loaded_from_the_committed_history()
            {
               List<ITaggregateIdentifyingTevent<ITaggregateTevent>> history = [];
               ((ITaggregate)_project).Commit(history.AddRange);
               _reloadedProject = ProjectTaggregate.LoadFromHistory(history);
            }

            [XF] public void the_reloaded_checklist_contains_both_items() => _reloadedProject.Checklist.Entities.InCreationOrder.Must().HaveCount(2);

            [XF] public void the_reloaded_items_titles_are_restored()
            {
               _reloadedProject.Checklist.Entities.InCreationOrder[0].Title.Must().NotBeNull().Be("Specify");
               _reloadedProject.Checklist.Entities.InCreationOrder[1].Title.Must().NotBeNull().Be("Implement");
            }

            [XF] public void only_the_completed_item_is_completed_in_the_reloaded_checklist()
            {
               _reloadedProject.Checklist.Entities.InCreationOrder[0].IsCompleted.Must().BeTrue();
               _reloadedProject.Checklist.Entities.InCreationOrder[1].IsCompleted.Must().BeFalse();
            }
         }
      }
   }
}
