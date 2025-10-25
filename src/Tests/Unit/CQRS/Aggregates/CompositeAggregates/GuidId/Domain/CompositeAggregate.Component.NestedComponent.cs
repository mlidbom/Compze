using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

partial class Component
{
    public class NestedComponent :
        Component.Component<
            NestedComponent,
            CompositeAggregateTevent.Component.NestedComponent.Implementation.Root,
            CompositeAggregateTevent.Component.NestedComponent.IRoot>
    {
        public NestedComponent(Component parent) : base(parent)
        {
            RegisterTeventAppliers()
               .For<CompositeAggregateTevent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public string Name { get; private set; } = string.Empty;

        public void Rename(string name) => Publish(new CompositeAggregateTevent.Component.NestedComponent.Implementation.Renamed(name));
    }
}
