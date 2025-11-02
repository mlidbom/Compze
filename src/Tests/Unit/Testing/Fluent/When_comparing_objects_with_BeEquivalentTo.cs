using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_comparing_objects_with_BeEquivalentTo : UniversalTestBase
{
   class TestObject(string publicValue, string internalValue, string privateValue)
   {
      public string PublicProperty { get; set; } = publicValue;
      internal string InternalProperty { get; set; } = internalValue;
      private readonly string PrivateField = privateValue;

      public string GetPrivateField() => PrivateField;
   }

   public class given_two_objects_that_differ_in_one_private_member_member : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObject _actual = new("public_expected", "internal_expected", "private_actual");
      readonly TestObject _expected = new("public_expected", "internal_expected", "private_expected");

      public class BeEquivalentTo_throws_AssertionFailedException : given_two_objects_that_differ_in_one_private_member_member
      {
         string ExceptionMessage() => Invoking(() => _actual.Must().BeEquivalentTo(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

         public class and_the_exception_message_ : BeEquivalentTo_throws_AssertionFailedException
         {
            [XF] public void _is() =>
               ExceptionMessage().Must().Be(""""
                                            --------------------------------------------------
                                            expected the object returned by the expression: 
                                            --------------------------------------------------
                                            _actual
                                            --------------------------------------------------
                                            to be equivalent to the object returned by the expression:
                                            --------------------------------------------------
                                            _expected
                                            --------------------------------------------------
                                            But it resulted in the Diff:
                                            --------------------------------------------------
                                            --- expected
                                            +++ actual
                                            @@ -1,6 +1,6 @@
                                             {
                                               "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                               "InternalProperty": "internal_expected",
                                            -  "PrivateField": "private_expected",
                                            +  "PrivateField": "private_actual",
                                               "PublicProperty": "public_expected"
                                             }
                                            
                                            --------------------------------------------------
                                            Actual was:
                                            --------------------------------------------------
                                            {
                                              "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                              "InternalProperty": "internal_expected",
                                              "PrivateField": "private_actual",
                                              "PublicProperty": "public_expected"
                                            }
                                            --------------------------------------------------
                                            Expected was:
                                            --------------------------------------------------
                                            {
                                              "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                              "InternalProperty": "internal_expected",
                                              "PrivateField": "private_expected",
                                              "PublicProperty": "public_expected"
                                            }
                                            --------------------------------------------------
                                            """");
         }
      }
   }

   public class given_two_objects_that_are_equivalent : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObject _actual = new("same", "same", "same");
      readonly TestObject _expected = new("same", "same", "same");

      public class BeEquivalentTo_does_not_throw : given_two_objects_that_are_equivalent
      {
         [XF] public void assertion_succeeds() => _actual.Must().BeEquivalentTo(_expected);
      }
   }

   public class given_two_objects_that_differ_only_in_public_members : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObject _actual = new("public1", "sameInternal", "samePrivate");
      readonly TestObject _expected = new("public2", "sameInternal", "samePrivate");

      public class BeEquivalentTo_throws : given_two_objects_that_differ_only_in_public_members
      {
         [XF] public void because_it_checks_all_members()
            => Invoking(() => _actual.Must().BeEquivalentTo(_expected)).Must().Throw<AssertionFailedException>();
      }

      public class BeEquivalentToInternal_throws : given_two_objects_that_differ_only_in_public_members
      {
         [XF] public void because_public_members_differ()
            => Invoking(() => _actual.Must().BeEquivalentToInternal(_expected)).Must().Throw<AssertionFailedException>();
      }

      public class BeEquivalentToPublic_throws : given_two_objects_that_differ_only_in_public_members
      {
         [XF] public void because_public_members_differ()
            => Invoking(() => _actual.Must().BeEquivalentToPublic(_expected)).Must().Throw<AssertionFailedException>();
      }
   }

   public class given_two_objects_that_differ_only_in_internal_members : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObject _actual = new("samePublic", "internal1", "samePrivate");
      readonly TestObject _expected = new("samePublic", "internal2", "samePrivate");

      public class BeEquivalentTo_throws : given_two_objects_that_differ_only_in_internal_members
      {
         [XF] public void because_it_checks_all_members()
            => Invoking(() => _actual.Must().BeEquivalentTo(_expected)).Must().Throw<AssertionFailedException>();
      }

      public class BeEquivalentToInternal_throws : given_two_objects_that_differ_only_in_internal_members
      {
         [XF] public void because_internal_members_differ()
            => Invoking(() => _actual.Must().BeEquivalentToInternal(_expected)).Must().Throw<AssertionFailedException>();
      }

      public class BeEquivalentToPublic_does_not_throw : given_two_objects_that_differ_only_in_internal_members
      {
         [XF] public void because_public_members_are_the_same() => _actual.Must().BeEquivalentToPublic(_expected);
      }
   }

   public class given_two_objects_that_differ_only_in_private_members : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObject _actual = new("samePublic", "sameInternal", "private1");
      readonly TestObject _expected = new("samePublic", "sameInternal", "private2");

      public class BeEquivalentTo_throws : given_two_objects_that_differ_only_in_private_members
      {
         [XF] public void because_it_checks_all_members()
            => Invoking(() => _actual.Must().BeEquivalentTo(_expected)).Must().Throw<AssertionFailedException>();
      }

      public class BeEquivalentToInternal_does_not_throw : given_two_objects_that_differ_only_in_private_members
      {
         [XF] public void because_it_checks_public_internal_and_protected_members_but_not_private() => _actual.Must().BeEquivalentToInternal(_expected);
      }

      public class BeEquivalentToPublic_does_not_throw : given_two_objects_that_differ_only_in_private_members
      {
         [XF] public void because_public_members_are_the_same() => _actual.Must().BeEquivalentToPublic(_expected);
      }
   }

   public class given_two_objects_with_different_Ids : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObjectWithId _actual = new(1, "sameValue");
      readonly TestObjectWithId _expected = new(2, "sameValue");

      class TestObjectWithId(int id, string value)
      {
         public int Id { get; } = id;
         public string Value { get; } = value;
      }

      public class BeEquivalentTo_with_exclusion_does_not_throw : given_two_objects_with_different_Ids
      {
         [XF] public void because_Id_is_excluded() 
            => _actual.Must().BeEquivalentTo(_expected, config => config.Excluding(obj => obj.Id));
      }

      public class BeEquivalentTo_without_exclusion_throws : given_two_objects_with_different_Ids
      {
         [XF] public void because_Id_differs()
            => Invoking(() => _actual.Must().BeEquivalentTo(_expected)).Must().Throw<AssertionFailedException>();
      }
   }

   public class given_a_collection_with_objects_having_different_Ids : When_comparing_objects_with_BeEquivalentTo
   {
      readonly List<TestObjectWithId> _actual = [new(1, "value1"), new(2, "value2")];
      readonly List<TestObjectWithId> _expected = [new(99, "value1"), new(88, "value2")];

      class TestObjectWithId(int id, string value)
      {
         public int Id { get; } = id;
         public string Value { get; } = value;
      }

      public class BeEquivalentTo_with_First_Id_exclusion_does_not_throw : given_a_collection_with_objects_having_different_Ids
      {
         [XF] public void because_Id_is_excluded_using_First()
            => _actual.Must().BeEquivalentTo(_expected, config => config.Excluding(list => list.First().Id));
      }

      public class BeEquivalentTo_with_indexer_Id_exclusion_does_not_throw : given_a_collection_with_objects_having_different_Ids
      {
         [XF] public void because_Id_is_excluded_using_indexer()
            => _actual.Must().BeEquivalentTo(_expected, config => config.Excluding(list => list[0].Id));
      }
   }

   public class given_nested_objects_with_different_Id_properties : When_comparing_objects_with_BeEquivalentTo
   {
      readonly Container _actual = new(new Inner(1, "value"), new Outer(1, "other"));
      readonly Container _expected = new(new Inner(99, "value"), new Outer(99, "other"));

      class Container(Inner inner, Outer outer)
      {
         public Inner Inner { get; } = inner;
         public Outer Outer { get; } = outer;
      }

      class Inner(int id, string value)
      {
         public int Id { get; } = id;
         public string Value { get; } = value;
      }

      class Outer(int id, string value)
      {
         public int Id { get; } = id;
         public string Value { get; } = value;
      }

      public class BeEquivalentTo_excluding_only_Outer_Id_throws : given_nested_objects_with_different_Id_properties
      {
         [XF] public void because_Inner_Id_differs_and_is_not_excluded()
            => Invoking(() => _actual.Must().BeEquivalentTo(_expected, config => config.Excluding(c => c.Outer.Id)))
               .Must().Throw<AssertionFailedException>();
      }

      public class BeEquivalentTo_excluding_only_Inner_Id_throws : given_nested_objects_with_different_Id_properties
      {
         [XF] public void because_Outer_Id_differs_and_is_not_excluded()
            => Invoking(() => _actual.Must().BeEquivalentTo(_expected, config => config.Excluding(c => c.Inner.Id)))
               .Must().Throw<AssertionFailedException>();
      }
   }
}
