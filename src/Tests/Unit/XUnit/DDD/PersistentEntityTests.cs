using System;
using Compze.Abstractions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;
using Xunit;

namespace Compze.Tests.Unit.XUnit.DDD;


#pragma warning disable CA1508 //Avoid dead conditional code

public class PersistentEntityTests : XUnitTestBase
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

   [XFact]
   public void InstanceEqualsItself()
   {
      var person = new Person();
      AssertAreEqual(person, person);
   }

   [XFact]
   public void IntstanceEqualsOtherInstanceWithSameId()
   {
      var lhs = new Person();
      var rhs = new Person(lhs.Id);
      AssertAreEqual(lhs, rhs);
   }

   [XFact]
   public void IntstanceNotEqualToinstanceWithOtherId()
   {
      var lhs = new Person(Guid.NewGuid());
      var rhs = new Person(Guid.NewGuid());
      AssertAreNotEqual(lhs, rhs);
   }

   [XFact]
   public void IntstancesWithSameIdHasSameHashCode()
   {
      var lhs = new Person();
      var rhs = new Person(lhs.Id);
      lhs.GetHashCode().Should().Be(rhs.GetHashCode());
      (lhs.GetHashCode() == rhs.GetHashCode()).Should().BeTrue();
   }


   [XFact]
   public void ComparisonWithRhsNullReturnsFalse()
   {
      var lhs = new Person();
      lhs.Equals(null!).Should().BeFalse();
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (lhs == null).Should().BeFalse();
   }

   [XFact]
   public void ComparisonWithLhsNullReturnsFalse()
   {
      var rhs = new Person();
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      (null == rhs).Should().BeFalse();
   }

   [XFact]
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
