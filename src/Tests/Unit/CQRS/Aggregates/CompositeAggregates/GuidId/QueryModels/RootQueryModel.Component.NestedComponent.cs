using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

partial class Component
{
   public class NestedComponent : Component.NestedComponent<NestedComponent, CompositeAggregateTevent.Component.NestedComponent.IRoot>
   {
      public NestedComponent(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<CompositeAggregateTevent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public string Name { get; private set; } = string.Empty;
   }
}