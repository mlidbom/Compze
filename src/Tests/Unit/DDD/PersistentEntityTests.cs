using System;
using Compze.Core.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.DDD;


#pragma warning disable CA1508 //Avoid dead conditional code

public class EntityTests : UniversalTestBase
{
   class Person : Entity<Person>
   {
      public Person()
      {
      }

      public Person(Guid id) : base(id)
      {
      }
   }

   [XF]
   public void InstanceEqualsItself()
   {
      var person = new Person();
      AssertAreEqual(person, person);
   }

   [XF]
   public void IntstanceNotEqualToinstanceWithOtherId()
   {
      var lhs = new Person(Guid.NewGuid());
      var rhs = new Person(Guid.NewGuid());
      AssertAreNotEqual(lhs, rhs);
   }


   [XF]
   public void ComparisonWithRhsNullReturnsFalse()
   {
      var lhs = new Person();
      lhs.Equals(null!).Must().BeFalse();
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (lhs == null).Must().BeFalse();
   }

   [XF]
   public void ComparisonWithLhsNullReturnsFalse()
   {
      var rhs = new Person();
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (null == rhs).Must().BeFalse();
   }

   [XF]
   public void ComparisonWithLhsNullAndRhsNullReturnsTrue()
   {
      Person? rhs = null;
      Person? lhs = null;
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (rhs == lhs).Must().BeTrue();
   }

   static void AssertAreEqual(Person lhs, Person rhs)
   {
      lhs.Must().Be(rhs);
      lhs.Equals(rhs).Must().BeTrue();
      Equals(lhs, rhs).Must().BeTrue();
      (lhs == rhs).Must().BeTrue();
      (lhs != rhs).Must().BeFalse();
   }

   static void AssertAreNotEqual(Person lhs, Person rhs)
   {
      lhs.Must().NotBe(rhs);
      lhs.Equals(rhs).Must().BeFalse();
      Equals(lhs, rhs).Must().BeFalse();
      (lhs == rhs).Must().BeFalse();
      (lhs != rhs).Must().BeTrue();
   }
}
