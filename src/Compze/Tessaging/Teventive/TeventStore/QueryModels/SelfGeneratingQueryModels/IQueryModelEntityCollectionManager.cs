namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

interface IQueryModelEntityCollectionManager<TEntity, in TEntityId>
{
   IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
}