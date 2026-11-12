// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications.Nullability;

/// <summary>
/// What a consumer's compiler says about an assertion, with nullable reference types enabled: an assertion accepts its<br/>
/// subject whatever nullability the subject is declared with, and returns a non-nullable context exactly when passing it<br/>
/// proves the value is not null. A nullable value type is a type of its own: <see cref="NullabilityAssertions.NotBeNull{TValue}(IAssertionContext{TValue?})"/><br/>
/// narrows it to its underlying type, which the assertions over that type take.
/// </summary>
public abstract class When_a_consumer_compiles_an_assertion : UniversalTestBase
{
   readonly string _parameters;

   ///<param name="parameters">The parameters of the consumer's method, which declare the subject, such as <c>string? subject</c>.</param>
   protected When_a_consumer_compiles_an_assertion(string parameters) => _parameters = parameters;

   private protected ConsumerCode Compiling(string statements) => ConsumerCode.Compiling(_parameters, statements);

   public class on_a_string_declared_nullable() : When_a_consumer_compiles_an_assertion("string? subject")
   {
      [XF] public void Be_with_a_string_compiles_without_warnings() => Compiling("""subject.Must().Be("text");""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void NotBe_with_a_string_compiles_without_warnings() => Compiling("""subject.Must().NotBe("text");""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeNull_compiles_without_warnings() => Compiling("subject.Must().BeNull();").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeNullOrEmpty_compiles_without_warnings() => Compiling("subject.Must().BeNullOrEmpty();").WarningsAndErrors.Must().BeEmpty();
      [XF] public void DeepEqual_with_a_string_compiles_without_warnings() => Compiling("""subject.Must().DeepEqual("text");""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void ReferenceEqual_with_a_string_compiles_without_warnings() => Compiling("""subject.Must().ReferenceEqual("text");""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeOneOf_two_strings_compiles_without_warnings() => Compiling("""subject.Must().BeOneOf(["a", "b"]);""").WarningsAndErrors.Must().BeEmpty();

      [XF] public void NotBeNull_narrows_the_context_to_a_non_nullable_string() => Compiling("string narrowed = subject.Must().NotBeNull().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void NotBeNullOrEmpty_narrows_the_context_to_a_non_nullable_string() => Compiling("string narrowed = subject.Must().NotBeNullOrEmpty().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void NotBeNullOrWhiteSpace_narrows_the_context_to_a_non_nullable_string() => Compiling("string narrowed = subject.Must().NotBeNullOrWhiteSpace().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void Contain_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().Contain("x").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void NotContain_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().NotContain("x").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void StartWith_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().StartWith("x").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void EndWith_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().EndWith("x").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeGreaterThan_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().BeGreaterThan("a").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeGreaterThanOrEqualTo_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().BeGreaterThanOrEqualTo("a").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeLessThan_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().BeLessThan("z").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeLessThanOrEqualTo_narrows_the_context_to_a_non_nullable_string() => Compiling("""string narrowed = subject.Must().BeLessThanOrEqualTo("z").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BePositive_narrows_the_context_to_a_non_nullable_string() => Compiling("string narrowed = subject.Must().BePositive().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeNegative_narrows_the_context_to_a_non_nullable_string() => Compiling("string narrowed = subject.Must().BeNegative().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeExactType_narrows_the_context_to_a_non_nullable_string() => Compiling("string narrowed = subject.Must().BeExactType<string>().Actual;").WarningsAndErrors.Must().BeEmpty();

      [XF] public void RunAssertionThatANullValueFails_hands_its_predicate_a_non_nullable_string_and_narrows_the_context_to_one() =>
         Compiling("string narrowed = subject.Must().RunAssertionThatANullValueFails(it => it.Length > 0, failureMessage: it => it.ToUpperInvariant()).Actual;").WarningsAndErrors.Must().BeEmpty();

      [XF] public void Satisfy_with_a_predicate_that_dereferences_the_value_warns_that_the_value_may_be_null() =>
         Compiling("subject.Must().Satisfy(it => it.Length == 1);").WarningAndErrorIds.Must().SequenceEqual(["CS8602"]);

      [XF] public void dereferencing_the_Actual_value_warns_that_it_may_be_null() =>
         Compiling("var length = subject.Must().Actual.Length;").WarningAndErrorIds.Must().SequenceEqual(["CS8602"]);
   }

   public class on_a_non_nullable_string() : When_a_consumer_compiles_an_assertion("string subject")
   {
      [XF] public void Be_keeps_the_context_non_nullable() => Compiling("""string kept = subject.Must().Be("text").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void Contain_keeps_the_context_non_nullable() => Compiling("""string kept = subject.Must().Contain("x").Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void Satisfy_with_a_predicate_that_dereferences_the_value_compiles_without_warnings() => Compiling("subject.Must().Satisfy(it => it.Length == 1);").WarningsAndErrors.Must().BeEmpty();
   }

   public class on_a_non_nullable_string_with_an_expected_string_declared_nullable() : When_a_consumer_compiles_an_assertion("string subject, string? expected")
   {
      [XF] public void Be_with_the_expected_string_compiles_without_warnings() => Compiling("subject.Must().Be(expected);").WarningsAndErrors.Must().BeEmpty();
      [XF] public void NotBe_with_the_expected_string_compiles_without_warnings() => Compiling("subject.Must().NotBe(expected);").WarningsAndErrors.Must().BeEmpty();
   }

   public class on_a_list_declared_nullable() : When_a_consumer_compiles_an_assertion("List<string>? subject")
   {
      [XF] public void HaveCount_narrows_the_context_to_a_non_nullable_list() => Compiling("List<string> narrowed = subject.Must().HaveCount(1).Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeEmpty_narrows_the_context_to_a_non_nullable_list() => Compiling("List<string> narrowed = subject.Must().BeEmpty().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void NotBeEmpty_narrows_the_context_to_a_non_nullable_list() => Compiling("List<string> narrowed = subject.Must().NotBeEmpty().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void SequenceEqual_narrows_the_context_to_a_non_nullable_list() => Compiling("""List<string> narrowed = subject.Must().SequenceEqual(new[] { "a" }).Actual;""").WarningsAndErrors.Must().BeEmpty();
      [XF] public void Contain_narrows_the_context_to_a_non_nullable_sequence() => Compiling("""IEnumerable<string> narrowed = subject.Must().Contain("a").Actual;""").WarningsAndErrors.Must().BeEmpty();
   }

   public class on_each_kind_of_set_and_read_only_collection_declared_nullable()
      : When_a_consumer_compiles_an_assertion("HashSet<string>? hashSet, ISet<string>? set, IReadOnlySet<string>? readOnlySet, ReadOnlyCollection<string>? readOnlyCollection")
   {
      [XF] public void Contain_narrows_the_context_to_the_non_nullable_collection() =>
         Compiling("""
                   HashSet<string> narrowedHashSet = hashSet.Must().Contain("a").Actual;
                   ISet<string> narrowedSet = set.Must().Contain("a").Actual;
                   IReadOnlySet<string> narrowedReadOnlySet = readOnlySet.Must().Contain("a").Actual;
                   ReadOnlyCollection<string> narrowedReadOnlyCollection = readOnlyCollection.Must().Contain("a").Actual;
                   """).WarningsAndErrors.Must().BeEmpty();
   }

   public class on_an_int_declared_nullable() : When_a_consumer_compiles_an_assertion("int? subject")
   {
      [XF] public void NotBeNull_narrows_the_context_to_an_int() => Compiling("int narrowed = subject.Must().NotBeNull().Actual;").WarningsAndErrors.Must().BeEmpty();
      [XF] public void Be_with_an_int_compiles_without_warnings() => Compiling("subject.Must().Be(1);").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeNull_compiles_without_warnings() => Compiling("subject.Must().BeNull();").WarningsAndErrors.Must().BeEmpty();
      [XF] public void the_comparisons_are_reached_through_NotBeNull() => Compiling("int narrowed = subject.Must().NotBeNull().BeGreaterThan(0).BePositive().Actual;").WarningsAndErrors.Must().BeEmpty();
   }

   public class on_a_bool_declared_nullable() : When_a_consumer_compiles_an_assertion("bool? subject")
   {
      [XF] public void BeTrue_is_reached_through_NotBeNull() => Compiling("subject.Must().NotBeNull().BeTrue();").WarningsAndErrors.Must().BeEmpty();
   }

   public class on_a_non_nullable_int() : When_a_consumer_compiles_an_assertion("int subject")
   {
      [XF] public void NotBeNull_compiles_without_warnings() => Compiling("subject.Must().NotBeNull();").WarningsAndErrors.Must().BeEmpty();
      [XF] public void BeGreaterThan_keeps_the_context_an_int() => Compiling("int kept = subject.Must().BeGreaterThan(0).Actual;").WarningsAndErrors.Must().BeEmpty();
   }
}
