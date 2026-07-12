using Compze.Teventive.Taggregates.BaseClasses;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

readonly struct ChecklistItemId(Guid value) : IEquatable<ChecklistItemId>
{
   readonly Guid _value = value;

   public static ChecklistItemId New() => new(Guid.NewGuid());

   public bool Equals(ChecklistItemId other) => _value == other._value;
   public override bool Equals(object? obj) => obj is ChecklistItemId other && Equals(other);
   public override int GetHashCode() => _value.GetHashCode();
   public override string ToString() => _value.ToString();
}

///<summary>The reusable-library side of the shared tentity fixture: a checklist item any taggregate can keep a checklist of.<br/>
/// Every tevent states the <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> of the item it belongs to as ordinary domain data.</summary>
interface IChecklistItemTevent : ISharedTentityTevent<ChecklistItemId>
{
#pragma warning disable CA1715 // Nested tevent interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
   interface Added : IChecklistItemTevent
   {
      string Title { get; }
   }

   interface Completed : IChecklistItemTevent;
#pragma warning restore CA1715
}

class ChecklistItemTevent(ChecklistItemId entityId) : IChecklistItemTevent
{
   public ChecklistItemId EntityId { get; } = entityId;

   internal class Added(ChecklistItemId entityId, string title) : ChecklistItemTevent(entityId), IChecklistItemTevent.Added
   {
      public string Title { get; } = title;
   }

   internal class Completed(ChecklistItemId entityId) : ChecklistItemTevent(entityId), IChecklistItemTevent.Completed;
}

class ChecklistItem : SharedTentity<ChecklistItem, ChecklistItemId, IChecklistItemTevent, IChecklistItemTevent.Added>
{
   public ChecklistItem(SharedTentityCollection<ChecklistItem, ChecklistItemId, IChecklistItemTevent, IChecklistItemTevent.Added> checklist) : base(checklist) =>
      RegisterTeventAppliers()
        .For<IChecklistItemTevent.Added>(tevent => Title = tevent.Title)
        .For<IChecklistItemTevent.Completed>(_ => IsCompleted = true);

   public string? Title { get; private set; }
   public bool IsCompleted { get; private set; }

   public void Complete() => Publish(new ChecklistItemTevent.Completed(Id));
}
