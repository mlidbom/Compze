using System;

namespace Compze.Abstractions.Public;

///<summary>Base class for persistent entities with versioning information</summary>
public class VersionedPersistentEntity<T> : PersistentEntity<T> where T : VersionedPersistentEntity<T>
{
   /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
   protected VersionedPersistentEntity(Guid id) : base(id)
   {
   }

   ///<summary>Contains the current version of the entity</summary>
   public virtual int Version { get; protected set; }
}