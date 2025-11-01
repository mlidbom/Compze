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

   public class given_two_objects_that_differ_in_all_members : When_comparing_objects_with_BeEquivalentTo
   {
      readonly TestObject _actual = new("public1", "internal1", "private1");
      readonly TestObject _expected = new("public2", "internal2", "private2");

      public class BeEquivalentTo_throws_AssertionFailedException : given_two_objects_that_differ_in_all_members
      {
         string ExceptionMessage() => Invoking(() => _actual.Must().BeEquivalentTo(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

         public class and_the_exception_message_contains : BeEquivalentTo_throws_AssertionFailedException
         {
            [XF] public void the_full_actual_expression() =>
               ExceptionMessage().Must().Contain($"""
                                                  expected the expression: 
                                                  --------------------------------------------------
                                                     {nameof(_actual)}
                                                  --------------------------------------------------
                                                  to BeEquivalentTo:
                                                  --------------------------------------------------
                                                     {nameof(_expected)}
                                                  --------------------------------------------------
                                                  """);

            [XF] public void the_full_Actual_ToString_section() =>
               ExceptionMessage().Must().Contain("""
                                                 Actual.ToString():
                                                 --------------------------------------------------
                                                    Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject
                                                 --------------------------------------------------
                                                 """);

            [XF] public void the_full_Expected_ToString_section() =>
               ExceptionMessage().Must().Contain("""
                                                 Expected.ToString():
                                                 --------------------------------------------------
                                                    Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject
                                                 --------------------------------------------------
                                                 """);

            [XF] public void the_complete_actual_json_with_heading_and_separators() =>
               ExceptionMessage().Must().Contain("""
                                                 Actual JSON:
                                                 --------------------------------------------------
                                                 {
                                                   "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                                   "PublicProperty": "public1",
                                                   "InternalProperty": "internal1",
                                                   "PrivateField": "private1"
                                                 }
                                                 --------------------------------------------------
                                                 """);

            [XF] public void the_complete_expected_json_with_heading_and_separators() =>
               ExceptionMessage().Must().Contain("""
                                                 Expected JSON:
                                                 --------------------------------------------------
                                                 {
                                                   "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                                   "PublicProperty": "public2",
                                                   "InternalProperty": "internal2",
                                                   "PrivateField": "private2"
                                                 }
                                                 --------------------------------------------------
                                                 """);

            [XF] public void the_full_unified_diff_with_heading_and_separators() =>
               ExceptionMessage().Must().Contain("""
                                                 JSON Diff:
                                                 --------------------------------------------------
                                                 --- expected
                                                 +++ actual
                                                 @@ -1,6 +1,6 @@
                                                  {
                                                    "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                                 -  "PublicProperty": "public2",
                                                 -  "InternalProperty": "internal2",
                                                 -  "PrivateField": "private2"
                                                 +  "PublicProperty": "public1",
                                                 +  "InternalProperty": "internal1",
                                                 +  "PrivateField": "private1"
                                                  }

                                                 --------------------------------------------------
                                                 """);

            [XF] public void the_full_message_must_be() =>
               ExceptionMessage().Must().Be("""
                                                 
                                                 expected the expression: 
                                                 --------------------------------------------------
                                                    _actual
                                                 --------------------------------------------------
                                                 to BeEquivalentTo:
                                                 --------------------------------------------------
                                                    _expected
                                                 --------------------------------------------------
                                                 
                                                 Actual.ToString():
                                                 --------------------------------------------------
                                                    Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject
                                                 --------------------------------------------------
                                                 
                                                 Expected.ToString():
                                                 --------------------------------------------------
                                                    Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject
                                                 --------------------------------------------------
                                                 
                                                 Actual JSON:
                                                 --------------------------------------------------
                                                 {
                                                   "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                                   "PublicProperty": "public1",
                                                   "InternalProperty": "internal1",
                                                   "PrivateField": "private1"
                                                 }
                                                 --------------------------------------------------
                                                 
                                                 Expected JSON:
                                                 --------------------------------------------------
                                                 {
                                                   "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                                   "PublicProperty": "public2",
                                                   "InternalProperty": "internal2",
                                                   "PrivateField": "private2"
                                                 }
                                                 --------------------------------------------------
                                                 
                                                 JSON Diff:
                                                 --------------------------------------------------
                                                 --- expected
                                                 +++ actual
                                                 @@ -1,6 +1,6 @@
                                                  {
                                                    "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                                 -  "PublicProperty": "public2",
                                                 -  "InternalProperty": "internal2",
                                                 -  "PrivateField": "private2"
                                                 +  "PublicProperty": "public1",
                                                 +  "InternalProperty": "internal1",
                                                 +  "PrivateField": "private1"
                                                  }
                                                 
                                                 --------------------------------------------------
                                                 """);
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

      public class BeEquivalentToInternal_does_not_throw : given_two_objects_that_differ_only_in_public_members
      {
         [XF] public void because_internal_members_are_the_same() => _actual.Must().BeEquivalentToInternal(_expected);
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
         [XF] public void because_it_only_checks_internal_and_protected_members() => _actual.Must().BeEquivalentToInternal(_expected);
      }

      public class BeEquivalentToPublic_does_not_throw : given_two_objects_that_differ_only_in_private_members
      {
         [XF] public void because_public_members_are_the_same() => _actual.Must().BeEquivalentToPublic(_expected);
      }
   }
}
