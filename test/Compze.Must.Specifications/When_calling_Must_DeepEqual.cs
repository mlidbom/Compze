

// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Must.Specifications;

public class When_calling_Must_DeepEqual : UniversalTestBase
{
   public class given_two_objects_that : When_calling_Must_DeepEqual
   {
      public class differ_in_one_private_member : given_two_objects_that
      {
         readonly TestObject _actual = new("public_expected", "internal_expected", "private_actual");
         readonly TestObject _expected = new("public_expected", "internal_expected", "private_expected");

         public class DeepEqual_throws_AssertionFailedException : differ_in_one_private_member
         {
            string ExceptionMessage() => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>().Which.Message;

            [XF] public void and_the_exception_message_is() =>
               ExceptionMessage().Must().Be(""""
                                            
                                            --------------------------------------------------
                                            Failing assertion:
                                            --------------------------------------------------
                                            _actual.Must().DeepEqual(_expected)
                                            --------------------------------------------------
                                            Diff:
                                            --------------------------------------------------
                                            --- expected
                                            +++ actual
                                            @@ -1,6 +1,6 @@
                                             {
                                               "$type": "Compze.Must.Specifications.When_calling_Must_DeepEqual+TestObject, Compze.Must.Specifications",
                                               "InternalProperty": "internal_expected",
                                            -  "PrivateField": "private_expected",
                                            +  "PrivateField": "private_actual",
                                               "PublicProperty": "public_expected"
                                             }
                                            
                                            --------------------------------------------------
                                            _actual was a Compze.Must.Specifications.When_calling_Must_DeepEqual.TestObject with:
                                            --------------------------------------------------
                                            JSON:
                                            --------------------------------------------------
                                            {
                                              "$type": "Compze.Must.Specifications.When_calling_Must_DeepEqual+TestObject, Compze.Must.Specifications",
                                              "InternalProperty": "internal_expected",
                                              "PrivateField": "private_actual",
                                              "PublicProperty": "public_expected"
                                            }
                                            --------------------------------------------------
                                            _expected was a Compze.Must.Specifications.When_calling_Must_DeepEqual.TestObject with:
                                            --------------------------------------------------
                                            JSON:
                                            --------------------------------------------------
                                            {
                                              "$type": "Compze.Must.Specifications.When_calling_Must_DeepEqual+TestObject, Compze.Must.Specifications",
                                              "InternalProperty": "internal_expected",
                                              "PrivateField": "private_expected",
                                              "PublicProperty": "public_expected"
                                            }
                                            --------------------------------------------------
                                            """");
         }
      }

      public class are_equivalent : given_two_objects_that
      {
         readonly TestObject _actual = new("same", "same", "same");
         readonly TestObject _expected = new("same", "same", "same");

         [XF] public void DeepEqual_does_not_throw() => _actual.Must().DeepEqual(_expected);
      }

      public class differ_in_public_members : given_two_objects_that
      {
         readonly TestObject _actual = new("public1", "sameInternal", "samePrivate");
         readonly TestObject _expected = new("public2", "sameInternal", "samePrivate");

         [XF] public void DeepEqual_throws()
            => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>();

         [XF] public void DeepEqualInternal_throws()
            => Invoking(() => _actual.Must().DeepEqualInternal(_expected)).Must().Throw<AssertionFailedException>();

         [XF] public void DeepEqualPublic_throws()
            => Invoking(() => _actual.Must().DeepEqualPublic(_expected)).Must().Throw<AssertionFailedException>();
      }

      public class differ_only_in_internal_members : given_two_objects_that
      {
         readonly TestObject _actual = new("samePublic", "internal1", "samePrivate");
         readonly TestObject _expected = new("samePublic", "internal2", "samePrivate");

         [XF] public void DeepEqual_throws()
            => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>();

         [XF] public void DeepEqualInternal_throws()
            => Invoking(() => _actual.Must().DeepEqualInternal(_expected)).Must().Throw<AssertionFailedException>();

         [XF] public void DeepEqualPublic_does_not_throw() => _actual.Must().DeepEqualPublic(_expected);
      }

      public class differ_only_in_private_members : given_two_objects_that
      {
         readonly TestObject _actual = new("samePublic", "sameInternal", "private1");
         readonly TestObject _expected = new("samePublic", "sameInternal", "private2");

         [XF] public void DeepEqual_throws()
            => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>();

         [XF] public void DeepEqualInternal_does_not_throw() => _actual.Must().DeepEqualInternal(_expected);

         [XF] public void DeepEqualPublic_does_not_throw() => _actual.Must().DeepEqualPublic(_expected);
      }

      public class have_different_Ids : given_two_objects_that
      {
         readonly TestObjectWithId _actual = new(1, "sameValue");
         readonly TestObjectWithId _expected = new(2, "sameValue");

         class TestObjectWithId(int id, string value)
         {
            public int Id { get; } = id;
            public string Value { get; } = value;
         }

         [XF] public void DeepEqual_throws()
            => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>();

         public class and_an_exclusion_of_the_Id_property : have_different_Ids
         {
            [XF] public void DeepEqual_does_not_throw_throws()
               => _actual.Must().DeepEqualPrivate(_expected, config => config.ExcludeTypeMember(obj => obj.Id));
         }
      }

      public class are_collections_with_objects_having_different_Ids : given_two_objects_that
      {
         readonly List<TestObjectWithId> _actual = [new(1, "value1"), new(2, "value2")];
         readonly List<TestObjectWithId> _expected = [new(99, "value1"), new(88, "value2")];

         class TestObjectWithId(int id, string value)
         {
            public int Id { get; } = id;
            public string Value { get; } = value;
         }

         [XF] public void DeepEqual_throws() => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>();

         public class with_an_exclusion_via_the_first_method : are_collections_with_objects_having_different_Ids
         {
            [XF] public void DeepEqual_does_not_throw()
               => _actual.Must().DeepEqualPrivate(_expected, config => config.ExcludeTypeMember(list => list.First().Id));
         }

         public class with_an_exclusion_via_the_indexer : are_collections_with_objects_having_different_Ids
         {
            [XF] public void DeepEqual_does_not_throw()
               => _actual.Must().DeepEqualPrivate(_expected, config => config.ExcludeTypeMember(list => list[0].Id));
         }
      }

      public class contain_nested_objects_with_differing_Id_properties_in_both_the_Inner_and_Outer_type : given_two_objects_that
      {
         readonly Container _actual = new(new Inner(1, "value"), new Outer(1, "other"));
         readonly Container _expected = new(new Inner(99, "value"), new Outer(99, "other"));

         [XF] public void DeepEqual_throws() => Invoking(() => _actual.Must().DeepEqual(_expected)).Must().Throw<AssertionFailedException>();

         public class And_an_exclusion_of_the_Id_property_in_the_Inner_class : contain_nested_objects_with_differing_Id_properties_in_both_the_Inner_and_Outer_type
         {
            [XF] public void DeepEqual_throws_()
               => Invoking(() => _actual.Must().DeepEqualPrivate(_expected, config => config.ExcludeTypeMember(c => c.Outer.Id)))
                 .Must().Throw<AssertionFailedException>();
         }

         public class And_an_exclusion_of_the_Id_property_in_the_Outer_class : contain_nested_objects_with_differing_Id_properties_in_both_the_Inner_and_Outer_type
         {
            [XF] public void DeepEqual_throws_()
               => Invoking(() => _actual.Must().DeepEqualPrivate(_expected, config => config.ExcludeTypeMember(c => c.Inner.Id)))
                 .Must().Throw<AssertionFailedException>();
         }

         public class and_an_exclusion_the_Id_property_in_both_the_Inner_class_and_Outer_class : contain_nested_objects_with_differing_Id_properties_in_both_the_Inner_and_Outer_type
         {
            [XF] public void DeepEqual_does_not_throw_throws()
               => _actual.Must().DeepEqualPrivate(_expected, config => config.ExcludeTypeMember(it => it.Outer.Id).ExcludeTypeMember(it => it.Inner.Id));
         }

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
      }
   }

   class TestObject(string publicValue, string internalValue, string privateValue)
   {
      public string PublicProperty { get; set; } = publicValue;
      internal string InternalProperty { get; set; } = internalValue;
      readonly string PrivateField = privateValue;

      public string GetPrivateField() => PrivateField;
   }
}
