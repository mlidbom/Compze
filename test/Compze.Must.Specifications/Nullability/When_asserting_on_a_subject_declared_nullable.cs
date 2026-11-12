// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications.Nullability;

/// <summary>
/// An assertion accepts a subject declared nullable, and a <see langword="null"/> value is just a value it reports: it fails<br/>
/// every assertion that needs a value, with an <see cref="AssertionFailedException"/>, and passes those that ask for null.<br/>
/// The context an assertion returns holds the subject's value, narrowed or not.
/// </summary>
public class When_asserting_on_a_subject_declared_nullable : UniversalTestBase
{
   public class that_is_a_null_string : When_asserting_on_a_subject_declared_nullable
   {
      readonly string? _subject = null;

      [XF] public void Be_with_a_string_throws_AssertionFailedException() => Invoking(() => _subject.Must().Be("text")).Must().Throw<AssertionFailedException>();
      [XF] public void Contain_throws_AssertionFailedException() => Invoking(() => _subject.Must().Contain("x")).Must().Throw<AssertionFailedException>();
      [XF] public void NotContain_throws_AssertionFailedException() => Invoking(() => _subject.Must().NotContain("x")).Must().Throw<AssertionFailedException>();
      [XF] public void StartWith_throws_AssertionFailedException() => Invoking(() => _subject.Must().StartWith("x")).Must().Throw<AssertionFailedException>();
      [XF] public void EndWith_throws_AssertionFailedException() => Invoking(() => _subject.Must().EndWith("x")).Must().Throw<AssertionFailedException>();
      [XF] public void BeGreaterThan_throws_AssertionFailedException() => Invoking(() => _subject.Must().BeGreaterThan("a")).Must().Throw<AssertionFailedException>();
      [XF] public void BeLessThan_throws_AssertionFailedException() => Invoking(() => _subject.Must().BeLessThan("z")).Must().Throw<AssertionFailedException>();
      [XF] public void BeNull_does_not_throw() => _subject.Must().BeNull();
      [XF] public void BeNullOrEmpty_does_not_throw() => _subject.Must().BeNullOrEmpty();

      [XF] public void the_message_of_a_failing_Contain_says_the_string_was_null() =>
         Invoking(() => _subject.Must().Contain("x")).Must().Throw<AssertionFailedException>().Which.Message.Must().Contain("_subject was null");

      [XF] public void the_message_of_a_failing_Be_says_the_string_was_null() =>
         Invoking(() => _subject.Must().Be("text")).Must().Throw<AssertionFailedException>().Which.Message.Must().Contain("_subject was null");
   }

   public class that_is_a_null_list : When_asserting_on_a_subject_declared_nullable
   {
      readonly List<int>? _subject = null;

      [XF] public void HaveCount_throws_AssertionFailedException() => Invoking(() => _subject.Must().HaveCount(0)).Must().Throw<AssertionFailedException>();
      [XF] public void BeEmpty_throws_AssertionFailedException() => Invoking(() => _subject.Must().BeEmpty()).Must().Throw<AssertionFailedException>();
      [XF] public void NotBeEmpty_throws_AssertionFailedException() => Invoking(() => _subject.Must().NotBeEmpty()).Must().Throw<AssertionFailedException>();
      [XF] public void SequenceEqual_throws_AssertionFailedException() => Invoking(() => _subject.Must().SequenceEqual(Array.Empty<int>())).Must().Throw<AssertionFailedException>();
      [XF] public void Contain_throws_AssertionFailedException() => Invoking(() => _subject.Must().Contain(1)).Must().Throw<AssertionFailedException>();
      [XF] public void BeNull_does_not_throw() => _subject.Must().BeNull();

      [XF] public void the_message_of_a_failing_HaveCount_says_the_count_was_null() =>
         Invoking(() => _subject.Must().HaveCount(0)).Must().Throw<AssertionFailedException>().Which.Message.Must().Contain("Expected count to be 0 but it was null");
   }

   public class that_is_a_null_set : When_asserting_on_a_subject_declared_nullable
   {
      readonly HashSet<int>? _subject = null;

      [XF] public void Contain_throws_AssertionFailedException() => Invoking(() => _subject.Must().Contain(1)).Must().Throw<AssertionFailedException>();
   }

   public class that_is_an_int_without_a_value : When_asserting_on_a_subject_declared_nullable
   {
      readonly int? _subject = null;

      [XF] public void NotBeNull_throws_AssertionFailedException() => Invoking(() => _subject.Must().NotBeNull()).Must().Throw<AssertionFailedException>();
      [XF] public void Be_with_an_int_throws_AssertionFailedException() => Invoking(() => _subject.Must().Be(1)).Must().Throw<AssertionFailedException>();
      [XF] public void BeNull_does_not_throw() => _subject.Must().BeNull();
   }

   public class that_holds_a_string : When_asserting_on_a_subject_declared_nullable
   {
      readonly string? _subject = "hello";

      [XF] public void Be_with_the_same_string_returns_a_context_holding_the_string() => _subject.Must().Be("hello").Actual.Must().ReferenceEqual(_subject);
      [XF] public void Contain_returns_a_context_holding_the_string() => _subject.Must().Contain("ell").Actual.Must().ReferenceEqual(_subject);
      [XF] public void NotContain_returns_a_context_holding_the_string() => _subject.Must().NotContain("xyz").Actual.Must().ReferenceEqual(_subject);
      [XF] public void StartWith_returns_a_context_holding_the_string() => _subject.Must().StartWith("he").Actual.Must().ReferenceEqual(_subject);
      [XF] public void EndWith_returns_a_context_holding_the_string() => _subject.Must().EndWith("lo").Actual.Must().ReferenceEqual(_subject);
      [XF] public void BeGreaterThan_returns_a_context_holding_the_string() => _subject.Must().BeGreaterThan("a").Actual.Must().ReferenceEqual(_subject);
   }

   public class that_holds_a_list : When_asserting_on_a_subject_declared_nullable
   {
      readonly List<int>? _subject = [1];

      [XF] public void HaveCount_returns_a_context_holding_the_list() => _subject.Must().HaveCount(1).Actual.Must().ReferenceEqual(_subject);
      [XF] public void NotBeEmpty_returns_a_context_holding_the_list() => _subject.Must().NotBeEmpty().Actual.Must().ReferenceEqual(_subject);
      [XF] public void SequenceEqual_returns_a_context_holding_the_list() => _subject.Must().SequenceEqual([1]).Actual.Must().ReferenceEqual(_subject);
      [XF] public void Contain_returns_a_context_holding_the_list() => _subject.Must().Contain(1).Actual.Must().ReferenceEqual(_subject);
   }

   public class that_is_an_int_with_a_value : When_asserting_on_a_subject_declared_nullable
   {
      readonly int? _subject = 42;

      [XF] public void NotBeNull_returns_a_context_holding_the_int() => _subject.Must().NotBeNull().Actual.Must().Be(42);
   }
}
