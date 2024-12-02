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
}