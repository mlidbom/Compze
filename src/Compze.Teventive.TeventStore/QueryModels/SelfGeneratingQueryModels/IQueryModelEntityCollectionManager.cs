namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public interface IQueryModelEntityCollectionManager<TEntity, in TEntityId> where TEntity : class
{
   IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
}