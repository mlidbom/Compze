using Compze.Abstractions;

namespace Compze.Teventive.StateStore.Storage;

///<summary>A taggregate that a unit of work deletes, as a state store tells its storage: enough to find what the storage keeps of it.</summary>
public sealed class TaggregateToDelete
{
   ///<summary>The taggregate of <paramref name="taggregateClass"/> with the id <paramref name="id"/>.</summary>
   public TaggregateToDelete(Type taggregateClass, TaggregateId id)
   {
      TaggregateClass = taggregateClass;
      Id = id;
   }

   ///<summary>The taggregate's class.</summary>
   public Type TaggregateClass { get; }

   ///<summary>The taggregate's id.</summary>
   public TaggregateId Id { get; }
}
