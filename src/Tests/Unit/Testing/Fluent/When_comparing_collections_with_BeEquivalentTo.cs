using System.Collections.Generic;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_comparing_unordered_collections_with_BeEquivalentTo : UniversalTestBase
{
   public class given_two_dictionaries_with_the_same_content_but_different_insertion_order : When_comparing_unordered_collections_with_BeEquivalentTo
   {
      readonly Dictionary<string, int> _dict1 = new() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
      readonly Dictionary<string, int> _dict2 = new() { ["c"] = 3, ["a"] = 1, ["b"] = 2 };

      public class BeEquivalentTo_should_not_throw : given_two_dictionaries_with_the_same_content_but_different_insertion_order
      {
         [XF] public void because_content_is_same_despite_different_order()
            => _dict1.Must().BeEquivalentTo(_dict2);
      }
   }

   public class given_two_hashsets_with_same_content_but_differing_insertion_order : When_comparing_unordered_collections_with_BeEquivalentTo
   {
      readonly HashSet<string> _set1 = ["apple", "banana", "cherry"];
      readonly HashSet<string> _set2 = ["cherry", "apple", "banana"];

      public class BeEquivalentTo_should_not_throw : given_two_hashsets_with_same_content_but_differing_insertion_order
      {
         [XF] public void because_content_is_same_despite_potentially_different_order()
            => _set1.Must().BeEquivalentTo(_set2);
      }
   }

   public class given_two_objects_with_hashset_properties_with_the_same_content_but_different_insertion_order : When_comparing_unordered_collections_with_BeEquivalentTo
   {
      readonly TestObject _obj1 = new() { Items = ["x", "y", "z"] };
      readonly TestObject _obj2 = new() { Items = ["z", "x", "y"] };

      class TestObject
      {
         public HashSet<string> Items { get; set; } = [];
      }

      public class BeEquivalentTo_should_not_throw : given_two_objects_with_hashset_properties_with_the_same_content_but_different_insertion_order
      {
         [XF] public void because_hashset_content_is_same()
            => _obj1.Must().BeEquivalentTo(_obj2);
      }
   }
}
