using System;
using Compze.Utilities.SystemCE;

namespace Compze.Core.DocumentDb.Private;

public partial class DocumentDbSession
{
   public class DocumentKey : IEquatable<DocumentKey>
   {
      internal DocumentKey(object id, Type type)
      {
         if(type.IsInterface)
         {
            throw new ArgumentException("Since a type can implement multiple interfaces using it to uniquely identify an instance is impossible");
         }
         Id = DocumentDb.GetIdString(id);
         Type = type;
      }

      public bool Equals(DocumentKey? other)
      {
         if(other == null)
         {
            return false;
         }

         if(!Equals(Id, other.Id))
         {
            return false;
         }

         return Type.IsAssignableFrom(other.Type) || other.Type.IsAssignableFrom(Type);
      }

      public override bool Equals(object? obj)
      {
         if (obj is null)
         {
            return false;
         }
         if (ReferenceEquals(this, obj))
         {
            return true;
         }
         if (obj is not DocumentKey key)
         {
            return false;
         }
         return Equals(key);
      }

      public override int GetHashCode() => Id.GetHashcodeCE();

      public override string ToString() => $"Id: {Id}, Type: {Type}";

      internal string Id { get; }
      Type Type { get;  }

   }

   public class DocumentKey<TDocument>(object id) : DocumentKey(id, typeof(TDocument));

}