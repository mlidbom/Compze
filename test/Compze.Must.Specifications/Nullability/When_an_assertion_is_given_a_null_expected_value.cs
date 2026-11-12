// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications.Nullability;

/// <summary>
/// A <see langword="null"/> expected value, where null can't be what anyone expects, is a bug in the test, not a failure of the code<br/>
/// under test: the assertion throws an <see cref="ArgumentNullException"/> naming the expected value's expression, whatever the subject.<br/>
/// The compiler warns about it first; these are the values that reach the assertion anyway, through a <c>!</c> or a suppression.
/// </summary>
public class When_an_assertion_is_given_a_null_expected_value : UniversalTestBase
{
   readonly string _nullString = null!;
   readonly int[] _nullSequence = null!;
   readonly int[] _aSequence = [1];

   [XF] public void Contain_throws_ArgumentNullException() => Invoking(() => "text".Must().Contain(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void NotContain_throws_ArgumentNullException() => Invoking(() => "text".Must().NotContain(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void StartWith_throws_ArgumentNullException() => Invoking(() => "text".Must().StartWith(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void EndWith_throws_ArgumentNullException() => Invoking(() => "text".Must().EndWith(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void SequenceEqual_throws_ArgumentNullException() => Invoking(() => _aSequence.Must().SequenceEqual(_nullSequence)).Must().Throw<ArgumentNullException>();
   [XF] public void BeGreaterThan_throws_ArgumentNullException() => Invoking(() => "b".Must().BeGreaterThan(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void BeGreaterThanOrEqualTo_throws_ArgumentNullException() => Invoking(() => "b".Must().BeGreaterThanOrEqualTo(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void BeLessThan_throws_ArgumentNullException() => Invoking(() => "b".Must().BeLessThan(_nullString)).Must().Throw<ArgumentNullException>();
   [XF] public void BeLessThanOrEqualTo_throws_ArgumentNullException() => Invoking(() => "b".Must().BeLessThanOrEqualTo(_nullString)).Must().Throw<ArgumentNullException>();

   [XF] public void the_exception_names_the_expected_value_s_expression() =>
      Invoking(() => "text".Must().StartWith(_nullString)).Must().Throw<ArgumentNullException>().Which.ParamName.Must().Be(nameof(_nullString));

   [XF] public void on_a_null_subject_it_still_throws_ArgumentNullException() =>
      Invoking(() => ((string?)null).Must().StartWith(_nullString)).Must().Throw<ArgumentNullException>();
}
