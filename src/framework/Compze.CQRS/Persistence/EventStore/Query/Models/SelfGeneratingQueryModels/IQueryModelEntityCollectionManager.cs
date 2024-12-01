namespace Compze.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

interface IQueryModelEntityCollectionManager<TEntity, in TEntityId>
{
   IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
}