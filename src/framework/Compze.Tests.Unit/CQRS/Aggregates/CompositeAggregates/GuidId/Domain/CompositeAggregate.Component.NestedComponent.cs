using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

partial class Component
{
   public class NestedComponent : Component.NestedComponent<NestedComponent, CompositeAggregateEvent.Component.NestedComponent.Implementation.Root, CompositeAggregateEvent.Component.NestedComponent.IRoot>
   {
      public NestedComponent(Component parent) : base(parent)
      {
         RegisterEventAppliers()
           .For<CompositeAggregateEvent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public string Name { get; private set; } = string.Empty;

      public void Rename(string name) => Publish(new CompositeAggregateEvent.Component.NestedComponent.Implementation.Renamed(name));
   }
}