using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

class RootQueryModel : SelfGeneratingQueryModel<RootQueryModel, ICompositeTaggregateTevent>, ITeventRouted
{
   static readonly TeventRouting<RootQueryModel, ICompositeTaggregateTevent> Appliers = DeclareAppliers(it => it
     .Component(static root => root.Component)
     .Entities(static root => root._entities)
     .For<ICompositeTaggregateTevent.PropertyUpdated.Name>(static (root, tevent) => root.Name = tevent.Name));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   public string Name { get; private set; } = string.Empty;
   readonly Entity.ICollectionManager _entities;
   public Component Component { get; private set; }

   public RootQueryModel()
   {
      Component = new Component(this);
      _entities = Entity.CreateSelfManagingCollection(this);
   }

   public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
}
