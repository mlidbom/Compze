using Compze.Sql.DocumentDb;
using FluentAssertions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

namespace Compze.Tests.Unit.Internals.XUnit.KeyValueStorage;


public class DocumentDBSession_DocumentKeyTests : XUnitTestBase
{
   class Base;

   // ReSharper disable once ClassNeverInstantiated.Local
   class Inheritor : Base;

   // ReSharper disable once ClassNeverInstantiated.Local
   class Unrelated;


   [XF]
   public void TwoInstancesOfTheSameTypeWithTheSameIdAreEqualAndHaveTheSameHashCode()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theId");

      lhs.Should().Be(rhs);
      rhs.Should().Be(lhs);
      lhs.GetHashCode().Should().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesOfTheSameTypeWithIdsDifferingOnlyByCaseAreEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("THEID");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theid");

      lhs.Should().Be(rhs);
      rhs.Should().Be(lhs);
      lhs.GetHashCode().Should().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesOfTheSameTypeWithIdsDifferingOnlyInTrailingSpacesAreEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theid  ");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theid");

      lhs.Should().Be(rhs);
      rhs.Should().Be(lhs);
      lhs.GetHashCode().Should().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesOfTheSameTypeWithDifferentIdsAreNotEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theSecondId");

      lhs.Should().NotBe(rhs);
      rhs.Should().NotBe(lhs);
   }

   [XF]
   public void TwoInstancesWithInheritingTypesAndTheSameIdAreEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
      var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theId");

      lhs.Should().Be(rhs);
      rhs.Should().Be(lhs);
      lhs.GetHashCode().Should().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesWithInheritingTypesAndDifferingIdsAreNotEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
      var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theSecondId");

      lhs.Should().NotBe(rhs);
      rhs.Should().NotBe(lhs);
   }

   [XF]
   public void TwoInstancesOfUnrelatedTypesAndSameIdAreNotEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
      var rhs = new DocumentDbSession.DocumentKey<Unrelated>("theId");

      lhs.Should().NotBe(rhs);
      rhs.Should().NotBe(lhs);
   }
}