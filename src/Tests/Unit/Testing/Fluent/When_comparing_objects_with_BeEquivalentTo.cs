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
                                            @@ -2,5 +2,5 @@
                                               "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                               "PublicProperty": "public_expected",
                                               "InternalProperty": "internal_expected",
                                            -  "PrivateField": "private_expected"
                                            +  "PrivateField": "private_actual"
                                             }
                                            
                                            --------------------------------------------------
                                            Actual was:
                                            --------------------------------------------------
                                            {
                                              "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                              "PublicProperty": "public_expected",
                                              "InternalProperty": "internal_expected",
                                              "PrivateField": "private_actual"
                                            }
                                            --------------------------------------------------
                                            Expected was:
                                            --------------------------------------------------
                                            {
                                              "$type": "Compze.Tests.Unit.Testing.Fluent.When_comparing_objects_with_BeEquivalentTo+TestObject, Compze.Tests.Unit",
                                              "PublicProperty": "public_expected",
                                              "InternalProperty": "internal_expected",
                                              "PrivateField": "private_expected"
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
