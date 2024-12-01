using System;
using Compze.DDD;
using Compze.Testing;
using NUnit.Framework;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Compze.Tests.DDD;

[TestFixture]
public class ValueObjectToString : UniversalTestBase
{
   class Root : ValueObject<Root>
   {
      public string Name { get; set; }
      public Branch Branch1 { get; set; }
      public Branch Branch2 { get; set; }
   }

   class Branch
   {
      public string Name { get; set; }
      public Leaf Leaf1 { get; set; }
      public Leaf Leaf2 { get; set; }
   }

   class Leaf
   {
      public string Name { get; set; }
   }

   [Test]
   public void ReturnsHierarchicalDescriptionOfData()
   {
      var description = new Root
                        {
                           Name = "RootName",
                           Branch1 = new Branch
                                     {
                                        Name = "Branch1Name",
                                        Leaf1 = new Leaf { Name = "Leaf1Name" },
                                        Leaf2 = new Leaf { Name = "Leaf2Name" }
                                     },
                           Branch2 = new Branch
                                     {
                                        Name = "Branch1Name",
                                        Leaf1 = new Leaf { Name = "Leaf1Name" },
                                        Leaf2 = new Leaf { Name = "Leaf2Name" }
                                     }
                        }.ToString();
      Assert.That(description, Is.EqualTo(
                     """Compze.Tests.DDD.ValueObjectToString+Root:{"Name":"RootName","Branch1":{"Name":"Branch1Name","Leaf1":{"Name":"Leaf1Name"},"Leaf2":{"Name":"Leaf2Name"}},"Branch2":{"Name":"Branch1Name","Leaf1":{"Name":"Leaf1Name"},"Leaf2":{"Name":"Leaf2Name"}}}"""
                       .Replace("\r\n","\n", StringComparison.Ordinal)
                       .Replace("\n",Environment.NewLine, StringComparison.Ordinal)));//Hack to get things working regardless of checkout line endings
   }
}