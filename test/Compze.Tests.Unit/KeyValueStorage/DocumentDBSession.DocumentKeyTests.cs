using Compze.DocumentDb.Private;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.KeyValueStorage;


public class DocumentDBSession_DocumentKeyTests : UniversalTestBase
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
   class Base;

   // ReSharper disable once ClassNeverInstantiated.Local
   class Inheritor : Base;

   // ReSharper disable once ClassNeverInstantiated.Local
   class Unrelated;
#pragma warning restore CA1812 // Avoid uninstantiated internal classes

   [XF]
   public void TwoInstancesOfTheSameTypeWithTheSameIdAreEqualAndHaveTheSameHashCode()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theId");

      lhs.Must().Be(rhs);
      rhs.Must().Be(lhs);
      lhs.GetHashCode().Must().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesOfTheSameTypeWithIdsDifferingOnlyByCaseAreEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("THEID");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theid");

      lhs.Must().Be(rhs);
      rhs.Must().Be(lhs);
      lhs.GetHashCode().Must().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesOfTheSameTypeWithIdsDifferingOnlyInTrailingSpacesAreEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theid  ");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theid");

      lhs.Must().Be(rhs);
      rhs.Must().Be(lhs);
      lhs.GetHashCode().Must().Be(rhs.GetHashCode());
   }

   [XF]
   public void TwoInstancesOfTheSameTypeWithDifferentIdsAreNotEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
      var rhs = new DocumentDbSession.DocumentKey<Base>("theSecondId");

      lhs.Must().NotBe(rhs);
      rhs.Must().NotBe(lhs);
   }

   [XF]
   public void TwoInstancesWithInheritingTypesAndTheSameIdAreNotEqual()
   {
      // A document is keyed by its exact concrete type, so a base-typed key and a derived-typed key with the
      // same id identify two distinct documents.
      var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
      var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theId");

      lhs.Must().NotBe(rhs);
      rhs.Must().NotBe(lhs);
   }

   [XF]
   public void TwoInstancesWithInheritingTypesAndDifferingIdsAreNotEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
      var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theSecondId");

      lhs.Must().NotBe(rhs);
      rhs.Must().NotBe(lhs);
   }

   [XF]
   public void TwoInstancesOfUnrelatedTypesAndSameIdAreNotEqual()
   {
      var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
      var rhs = new DocumentDbSession.DocumentKey<Unrelated>("theId");

      lhs.Must().NotBe(rhs);
      rhs.Must().NotBe(lhs);
   }
}
