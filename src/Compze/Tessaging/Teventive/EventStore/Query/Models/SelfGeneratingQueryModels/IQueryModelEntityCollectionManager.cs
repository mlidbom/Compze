namespace Compze.Tessaging.Teventive.EventStore.Tuery.Models.SelfGeneratingQueryModels;

interface IQueryModelEntityCollectionManager<TEntity, in TEntityId>
{
   IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
}