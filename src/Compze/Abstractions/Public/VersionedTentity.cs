using System;

namespace Compze.Core.Public;

///<summary>Base class for persistent entities with versioning information</summary>
public class VersionedEentity<T> : Entity<T> where T : VersionedEentity<T>
{
   /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
   protected VersionedEentity(Guid id):base(new EntityId(id))
   {
   }

   protected VersionedEentity(TentityId id):base(id)
   {
   }


   ///<summary>Contains the current version of the entity</summary>
   public virtual int Version { get; protected set; }
}