using System.Collections.Generic;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Tests.Testing.Fluent;

public class When_calling_Must_DeepEqual_with_unordered_collections : UniversalTestBase
{
   public class given_two_dictionaries: When_calling_Must_DeepEqual_with_unordered_collections
   {
      readonly Dictionary<string, int> _expected = new() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

      public class with_the_same_content_but_differing_orders : given_two_dictionaries
      {
         readonly Dictionary<string, int> _same_content_different_order = new() { ["c"] = 3, ["a"] = 1, ["b"] = 2 };

         [XF] public void be_equivalent_to_does_not_throw()
            => _expected.Must().DeepEqual(_same_content_different_order);
      }

      public class with_on_item_different : given_two_dictionaries
      {
         readonly Dictionary<string, int> _one_differing_item = new() { ["c"] = 3, ["a"] = 2, ["b"] = 2 };

         public class DeepEqual_throws_AssertionFailedException_ : with_on_item_different
         {
            string ExceptionMessage() => MustActions.Invoking(() => _expected.Must().DeepEqual(_one_differing_item)).Must().Throw<AssertionFailedException>().Which.Message;
            [XF] public void with_the_message_()
               => ExceptionMessage().Must().Be("""
                                               
                                               --------------------------------------------------
                                               expected:
                                               --------------------------------------------------
                                                  _expected
                                               --------------------------------------------------
                                               to be deeply equal to:
                                               --------------------------------------------------
                                                  _one_differing_item
                                               --------------------------------------------------
                                               But comparison of the objects serialized as JSON resulted in the Diff:
                                               --------------------------------------------------
                                               --- expected
                                               +++ actual
                                               @@ -1,6 +1,6 @@
                                                {
                                                  "$type": "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib",
                                               -  "a": 2,
                                               +  "a": 1,
                                                  "b": 2,
                                                  "c": 3
                                                }
                                               
                                               --------------------------------------------------
                                               Actual was:
                                               --------------------------------------------------
                                               {
                                                 "$type": "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib",
                                                 "a": 1,
                                                 "b": 2,
                                                 "c": 3
                                               }
                                               --------------------------------------------------
                                               Expected was:
                                               --------------------------------------------------
                                               {
                                                 "$type": "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib",
                                                 "a": 2,
                                                 "b": 2,
                                                 "c": 3
                                               }
                                               --------------------------------------------------
                                               """);
         }
      }
   }

   public class given_two_hashsets_with_same_content_but_differing_insertion_order : When_calling_Must_DeepEqual_with_unordered_collections
   {
      readonly HashSet<string> _set1 = ["apple", "banana", "cherry"];
      readonly HashSet<string> _set2 = ["cherry", "apple", "banana"];

      public class DeepEqual_should_not_throw : given_two_hashsets_with_same_content_but_differing_insertion_order
      {
         [XF] public void because_content_is_same_despite_potentially_different_order()
            => _set1.Must().DeepEqual(_set2);
      }
   }

   public class given_two_objects_with_hashset_properties_with_the_same_content_but_different_insertion_order : When_calling_Must_DeepEqual_with_unordered_collections
   {
      readonly TestObject _obj1 = new() { Items = ["x", "y", "z"] };
      readonly TestObject _obj2 = new() { Items = ["z", "x", "y"] };

      class TestObject
      {
         public HashSet<string> Items { get; set; } = [];
      }

      public class DeepEqual_should_not_throw : given_two_objects_with_hashset_properties_with_the_same_content_but_different_insertion_order
      {
         [XF] public void because_hashset_content_is_same()
            => _obj1.Must().DeepEqual(_obj2);
      }
   }
}
