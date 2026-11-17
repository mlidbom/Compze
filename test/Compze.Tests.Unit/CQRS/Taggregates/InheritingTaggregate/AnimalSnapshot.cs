using Compze.Abstractions;

namespace Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;

///<summary>An animal's whole state, whichever animal's class it is. It doesn't override Equals: the comparison of two is<br/>
/// <see cref="HoldTheSameState"/>, given with the class.</summary>
class AnimalSnapshot
{
   public AnimalSnapshot(TaggregateId id, DateTime timeOfBirth)
   {
      Id = id;
      TimeOfBirth = timeOfBirth;
   }

   public TaggregateId Id { get; }
   public DateTime TimeOfBirth { get; }

   public static bool HoldTheSameState(AnimalSnapshot one, AnimalSnapshot other) => one.Id == other.Id && one.TimeOfBirth == other.TimeOfBirth;
}
