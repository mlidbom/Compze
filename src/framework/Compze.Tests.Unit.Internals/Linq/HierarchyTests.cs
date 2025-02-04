using System.Collections.Generic;
using System.Linq;
using Compze.GenericAbstractions.Hierarchies;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.Unit.Internals.Linq;

[TestFixture]
public class HierarchyTests : UniversalTestBase
{
   class Hierarchical
   {
      public Hierarchical[] Children = [];
   }

   [Test]
   public void ShouldReturnAllInstancesInGraphWithoutDuplicates()
   {
      var root1 = new Hierarchical
                  {
                     Children =
                     [
                        new Hierarchical
                        {
                           Children =
                           [
                              new Hierarchical(),
                              new Hierarchical()
                           ]
                        },
                        new Hierarchical()
                     ]
                  };
      var root2 = new Hierarchical
                  {
                     Children =
                     [
                        new Hierarchical
                        {
                           Children =
                           [
                              new Hierarchical(),
                              new Hierarchical()
                           ]
                        },
                        new Hierarchical()
                     ]
                  };

      var flattened = EnumerableCE.Create(root1, root2).FlattenHierarchy(root => root.Children).ToList();
      Assert.That(flattened.Count, Is.EqualTo(10));            //Ensures no duplicates
      Assert.That(flattened.Distinct().Count(), Is.EqualTo(10)); //Ensures all objects are there.
   }

   class Person : IHierarchy<Person>
   {
      public IList<Person> Children = new List<Person>();
      IEnumerable<Person> IHierarchy<Person>.Children => Children;
   }


   [Test]
   public void FlatteningAHierarchicalTypeShouldWork()
   {
      var family = new Person
                   {
                      Children = new List<Person>
                                 {
                                    new()
                                    {
                                       Children = new List<Person>
                                                  {
                                                     new(),
                                                     new()
                                                  }
                                    },
                                    new()
                                 }
                   };
      var familyRegister = family.Flatten().ToList();
      Assert.That(familyRegister.Count, Is.EqualTo(5), "Should have 5 persons in the list");
      Assert.That(familyRegister.Count, Is.EqualTo(5), "Should have 5 unique persons in the list");
   }
}