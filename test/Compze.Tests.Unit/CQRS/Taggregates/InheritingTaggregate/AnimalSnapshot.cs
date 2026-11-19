using Compze.Abstractions;

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

///<summary>An animal's whole state, whichever animal's class it is. It doesn't override Equals, which neither a state store nor the<br/>
/// round-trip check needs.</summary>
class AnimalSnapshot
{
   public AnimalSnapshot(TaggregateId id, DateTime timeOfBirth)
   {
      Id = id;
      TimeOfBirth = timeOfBirth;
   }

   public TaggregateId Id { get; }
   public DateTime TimeOfBirth { get; }
}
