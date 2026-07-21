namespace Compze.DocumentDb._private;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class DocumentDbSession
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

      // A document is identified by the exact (Id, concrete type) pair: the same id may hold one document per
      // concrete type. Equality is therefore exact — no assignability/polymorphic matching across the hierarchy.
      public bool Equals(DocumentKey? other) => other != null && Equals(Id, other.Id) && Type == other.Type;

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

      public override int GetHashCode() => Id.GetHashcodeOrdinal();

      public override string ToString() => $"Id: {Id}, Type: {Type}";

      internal string Id { get; }
      Type Type { get;  }

   }
}
