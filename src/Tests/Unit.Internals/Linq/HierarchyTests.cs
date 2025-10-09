using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.GenericAbstractions.Hierarchies;
using Compze.Utilities.SystemCE.LinqCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using FluentAssertions;

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
      flattened.Count.Should().Be(10);            //Ensures no duplicates
      flattened.Distinct().Count().Should().Be(10); //Ensures all objects are there.
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
      familyRegister.Count.Should().Be(5, "Should have 5 persons in the list");
      familyRegister.Count.Should().Be(5, "Should have 5 unique persons in the list");
   }
}