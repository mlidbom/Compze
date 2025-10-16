using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

partial class Component
{
   public class NestedComponent : Component.NestedComponent<NestedComponent, CompositeAggregateEvent.Component.NestedComponent.IRoot>
   {
      public NestedComponent(Component parent) : base(parent)
      {
         RegisterEventAppliers()
           .For<CompositeAggregateEvent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public string Name { get; private set; } = string.Empty;
   }
}