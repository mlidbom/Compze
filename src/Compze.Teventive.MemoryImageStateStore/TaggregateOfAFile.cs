using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>The taggregate a file of <see cref="ITaggregateJsonFiles"/> is the file of: its class and its id. Equal to another of the<br/>
/// same class and id.</summary>
public sealed class TaggregateOfAFile : IEquatable<TaggregateOfAFile>
{
   internal TaggregateOfAFile(Type @class, TaggregateId id)
   {
      Class = @class;
      Id = id;
   }

   ///<summary>The taggregate's class: the class whose folder the file is in.</summary>
   public Type Class { get; }

   ///<summary>The taggregate's id: the id the file is named by.</summary>
   public TaggregateId Id { get; }

   ///<summary>Whether <paramref name="other"/> is of the same class and id.</summary>
   public bool Equals(TaggregateOfAFile? other) => other is not null && Class == other.Class && Id == other.Id;

   ///<inheritdoc/>
   public override bool Equals(object? obj) => obj is TaggregateOfAFile other && Equals(other);

   ///<inheritdoc/>
   public override int GetHashCode() => HashCode.Combine(Class, Id);

   ///<summary>Whether the two are of the same class and id, or both null.</summary>
   public static bool operator ==(TaggregateOfAFile? left, TaggregateOfAFile? right) => Equals(left, right);

   ///<summary>Whether the two are of another class or id, or only one is null.</summary>
   public static bool operator !=(TaggregateOfAFile? left, TaggregateOfAFile? right) => !Equals(left, right);
}
