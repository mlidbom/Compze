using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

partial class Component
{
    public class NestedComponent :
        Component.Component<
            NestedComponent,
            CompositeTaggregateTevent.Component.NestedComponent.Implementation.Root,
            CompositeTaggregateTevent.Component.NestedComponent.IRoot>
    {
        public NestedComponent(Component parent) : base(parent)
        {
            RegisterTeventAppliers()
               .For<CompositeTaggregateTevent.Component.NestedComponent.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public string Name { get; private set; } = string.Empty;

        public void Rename(string name) => Publish(new CompositeTaggregateTevent.Component.NestedComponent.Implementation.Renamed(name));
    }
}
