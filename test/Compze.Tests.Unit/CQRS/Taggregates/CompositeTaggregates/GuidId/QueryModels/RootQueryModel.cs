using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

class RootQueryModel : SelfGeneratingQueryModel<RootQueryModel, ICompositeTaggregateTevent>
{
   public string Name { get; private set; } = string.Empty;
   readonly Entity.CollectionManager _entities;
   public Component Component { get; private set; }

   public RootQueryModel()
   {
      Component = new Component(this);
      _entities = Entity.CreateSelfManagingCollection(this);

      RegisterTeventAppliers()
        .For<ICompositeTaggregateTevent.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
}