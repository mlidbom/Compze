using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component
{
   public class NestedComponent : RootQueryModel.Component<NestedComponent, ICompositeTaggregateTevent.Component.NestedComponent>, ITeventRouted
   {
      static readonly TeventRouting<NestedComponent, ICompositeTaggregateTevent.Component.NestedComponent> NestedComponentAppliers = DeclareAppliers(it => it
        .For<ICompositeTaggregateTevent.Component.NestedComponent.PropertyUpdated.Name>(static (component, tevent) => component.Name = tevent.Name));

      TeventRouting ITeventRouted.TeventAppliers => NestedComponentAppliers;

      public NestedComponent(Component parent) : base(parent) {}

      public string Name { get; private set; } = string.Empty;
   }
}
