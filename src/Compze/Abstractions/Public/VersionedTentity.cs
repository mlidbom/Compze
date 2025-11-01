namespace Compze.Core.Public;

///<summary>Base class for persistent entities with versioning information</summary>
public class VersionedEntity<T> : Entity<T> where T : VersionedEntity<T>
{
   protected VersionedEntity(TentityId id) : base(id) {}

   ///<summary>Contains the current version of the entity</summary>
   public virtual int Version { get; protected set; }
}
