using System.Collections.ObjectModel;
using Compze.Must.Assertions;

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_Contain : UniversalTestBase
{
   static readonly int[] ReadOnlyCollectionInts = [10, 20, 30];

   public class with_a_IReadOnlySet_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_a_IReadOnlySet_that
      {
         readonly IReadOnlySet<int> _set = new HashSet<int> { 1, 2, 3 };
         [XF] public void it_does_not_throw() => _set.Must().Contain(2);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly IReadOnlySet<int> _set = new HashSet<int> { 1, 2, 3 };
         [XF] public void it_throws() => Invoking(() => _set.Must().Contain(4)).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_an_ISet_that : When_calling_Must_Contain
   {
      public class that_contains_the_item : with_an_ISet_that
      {
         readonly ISet<string> _set = new HashSet<string> { "a", "b", "c" };
         [XF] public void it_does_not_throw() => _set.Must().Contain("b");
      }

      public class that_does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly ISet<string> _set = new HashSet<string> { "a", "b", "c" };
         [XF] public void it_throws() => Invoking(() => _set.Must().Contain("d")).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_a_HashSet_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_a_HashSet_that
      {
         readonly HashSet<double> _set = [1.1, 2.2, 3.3];
         [XF] public void it_does_not_throw() => _set.Must().Contain(2.2);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly HashSet<double> _set = [1.1, 2.2, 3.3];
         [XF] public void it_throws() => Invoking(() => _set.Must().Contain(4.4)).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_an_IEnumerable_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_an_IEnumerable_that
      {
         readonly IEnumerable<object> _collection = new List<object> { "hello", 42, true };
         [XF] public void it_does_not_throw() => _collection.Must().Contain(42);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly IEnumerable<object> _collection = new List<object> { "hello", 42, true };
         [XF] public void it_throws() => Invoking(() => _collection.Must().Contain("world")).Must().Throw<AssertionFailedException>();
      }
   }

   public class with_a_ReadOnlyCollection_that : When_calling_Must_Contain
   {
      public class contains_the_item : with_a_ReadOnlyCollection_that
      {
         readonly ReadOnlyCollection<int> _collection = new(ReadOnlyCollectionInts);
         [XF] public void it_does_not_throw() => _collection.Must().Contain(20);
      }

      public class does_not_contain_the_item : When_calling_Must_Contain
      {
         readonly ReadOnlyCollection<int> _collection = new(ReadOnlyCollectionInts);
         [XF] public void it_throws() => Invoking(() => _collection.Must().Contain(40)).Must().Throw<AssertionFailedException>();
      }
   }
}
