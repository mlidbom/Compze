namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

internal interface IQueryModelEntityCollectionManager<TEntity, in TEntityId>
{
   IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
}