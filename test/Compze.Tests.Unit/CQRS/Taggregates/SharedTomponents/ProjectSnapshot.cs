using Compze.Abstractions;

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>A project's whole state: its checklist of shared tentities. It doesn't override Equals, which neither a state store nor<br/>
/// the round-trip check needs.</summary>
class ProjectSnapshot
{
   public ProjectSnapshot(TaggregateId id, IReadOnlyList<ChecklistItemSnapshot> checklist)
   {
      Id = id;
      Checklist = checklist;
   }

   public TaggregateId Id { get; }
   public IReadOnlyList<ChecklistItemSnapshot> Checklist { get; }
}

///<summary>The state of an item in a project's checklist.</summary>
class ChecklistItemSnapshot
{
   public ChecklistItemSnapshot(ChecklistItemId id, string? title, bool isCompleted)
   {
      Id = id;
      Title = title;
      IsCompleted = isCompleted;
   }

   public ChecklistItemId Id { get; }
   public string? Title { get; }
   public bool IsCompleted { get; }
}
