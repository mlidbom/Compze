using System.Collections.Generic;
using System.Collections.ObjectModel;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must_Contain = Compze.Utilities.Testing.Fluent.Must_Contain;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_Contain : UniversalTestBase
{
   static readonly int[] ReadOnlyCollectionInts = [10, 20, 30];

   public class with_a_IReadOnlySet_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_a_IReadOnlySet_that
      {
         readonly IReadOnlySet<int> _set = new HashSet<int> { 1, 2, 3 };
         [XF] public void it_does_not_throw() => Must_Contain.Contain(__Must.Must(_set), 2);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly IReadOnlySet<int> _set = new HashSet<int> { 1, 2, 3 };
         [XF] public void it_throws() => Invoking(() => Must_Contain.Contain(__Must.Must(_set), 4)).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_an_ISet_that : When_calling_Must_Contain
   {
      public class that_contains_the_item : with_an_ISet_that
      {
         readonly ISet<string> _set = new HashSet<string> { "a", "b", "c" };
         [XF] public void it_does_not_throw() => Must_Contain.Contain(__Must.Must(_set), "b");
      }

      public class that_does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly ISet<string> _set = new HashSet<string> { "a", "b", "c" };
         [XF] public void it_throws() => Invoking(() => Must_Contain.Contain(__Must.Must(_set), "d")).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_a_HashSet_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_a_HashSet_that
      {
         readonly HashSet<double> _set = new() { 1.1, 2.2, 3.3 };
         [XF] public void it_does_not_throw() => Must_Contain.Contain(__Must.Must(_set), 2.2);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly HashSet<double> _set = new() { 1.1, 2.2, 3.3 };
         [XF] public void it_throws() => Invoking(() => Must_Contain.Contain(__Must.Must(_set), 4.4)).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_an_IEnumerable_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_an_IEnumerable_that
      {
         readonly IEnumerable<object> _collection = new List<object> { "hello", 42, true };
         [XF] public void it_does_not_throw() => Must_Contain.Contain(__Must.Must(_collection), 42);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly IEnumerable<object> _collection = new List<object> { "hello", 42, true };
         [XF] public void it_throws() => Invoking(() => Must_Contain.Contain(__Must.Must(_collection), "world")).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_a_ReadOnlyCollection_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_a_ReadOnlyCollection_that
      {
         readonly ReadOnlyCollection<int> _collection = new(ReadOnlyCollectionInts);
         [XF] public void it_does_not_throw() => Must_Contain.Contain(__Must.Must(_collection), 20);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly ReadOnlyCollection<int> _collection = new(ReadOnlyCollectionInts);
         [XF] public void it_throws() => Invoking(() => Must_Contain.Contain(__Must.Must(_collection), 40)).Must().Throw<AssertionFailedException>();
      }
   }
}
