namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

interface IQueryModelEntityCollectionManager<TEntity, in TEntityId>
{
   IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
}