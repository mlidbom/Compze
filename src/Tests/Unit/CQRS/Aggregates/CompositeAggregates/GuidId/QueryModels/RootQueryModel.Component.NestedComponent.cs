using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component
{
   public class NestedComponent : Component.NestedComponent<NestedComponent, CompositeTaggregateTevent.Component.NestedComponent.IRoot>
   {
      public NestedComponent(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<CompositeTaggregateTevent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public string Name { get; private set; } = string.Empty;
   }
}