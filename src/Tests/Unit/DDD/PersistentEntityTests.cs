using System;
using Compze.Core.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;

namespace Compze.Tests.Unit.DDD;


#pragma warning disable CA1508 //Avoid dead conditional code

public class PersistentEntityTests : UniversalTestBase
{
   class Person : PersistentEntity<Person>
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
   public void IntstanceEqualsOtherInstanceWithSameId()
   {
      var lhs = new Person();
      var rhs = new Person(lhs.Id);
      AssertAreEqual(lhs, rhs);
   }

   [XF]
   public void IntstanceNotEqualToinstanceWithOtherId()
   {
      var lhs = new Person(Guid.NewGuid());
      var rhs = new Person(Guid.NewGuid());
      AssertAreNotEqual(lhs, rhs);
   }

   [XF]
   public void IntstancesWithSameIdHasSameHashCode()
   {
      var lhs = new Person();
      var rhs = new Person(lhs.Id);
      lhs.GetHashCode().Should().Be(rhs.GetHashCode());
      (lhs.GetHashCode() == rhs.GetHashCode()).Should().BeTrue();
   }


   [XF]
   public void ComparisonWithRhsNullReturnsFalse()
   {
      var lhs = new Person();
      lhs.Equals(null!).Should().BeFalse();
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (lhs == null).Should().BeFalse();
   }

   [XF]
   public void ComparisonWithLhsNullReturnsFalse()
   {
      var rhs = new Person();
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (null == rhs).Should().BeFalse();
   }

   [XF]
   public void ComparisonWithLhsNullAndRhsNullReturnsTrue()
   {
      Person? rhs = null;
      Person? lhs = null;
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (rhs == lhs).Should().BeTrue();
   }

   static void AssertAreEqual(Person lhs, Person rhs)
   {
      lhs.Should().Be(rhs);
      lhs.Equals(rhs).Should().BeTrue();
      Equals(lhs, rhs).Should().BeTrue();
      (lhs == rhs).Should().BeTrue();
      (lhs != rhs).Should().BeFalse();
   }

   static void AssertAreNotEqual(Person lhs, Person rhs)
   {
      lhs.Should().NotBe(rhs);
      lhs.Equals(rhs).Should().BeFalse();
      Equals(lhs, rhs).Should().BeFalse();
      (lhs == rhs).Should().BeFalse();
      (lhs != rhs).Should().BeTrue();
   }
}
