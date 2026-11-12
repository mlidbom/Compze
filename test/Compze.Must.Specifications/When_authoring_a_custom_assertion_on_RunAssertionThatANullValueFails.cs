// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

///<summary>The extension point for a custom assertion that needs a value to check: an extension method on an<br/>
/// <see cref="IAssertionContext{T}"/> of a nullable type, calling <see cref="AssertionPrimitive.RunAssertionThatANullValueFails{T}"/>.</summary>
public class When_authoring_a_custom_assertion_on_RunAssertionThatANullValueFails : UniversalTestBase
{
   public class on_a_subject_that_passes : When_authoring_a_custom_assertion_on_RunAssertionThatANullValueFails
   {
      readonly string? _subject = "ada@example.com";

      [XF] public void the_returned_context_holds_the_subject_s_value() => _subject.Must().BeAnEmailAddress().Actual.Must().ReferenceEqual(_subject);
   }

   public class on_a_subject_that_fails_the_predicate : When_authoring_a_custom_assertion_on_RunAssertionThatANullValueFails
   {
      readonly string? _subject = "ada.example.com";

      [XF] public void it_throws_AssertionFailedException_with_the_heading_naming_the_custom_assertion() =>
         Invoking(() => _subject.Must().BeAnEmailAddress()).Must().Throw<AssertionFailedException>().Which.Message.Must().Contain("_subject.Must().BeAnEmailAddress()");
   }

   public class on_a_null_subject : When_authoring_a_custom_assertion_on_RunAssertionThatANullValueFails
   {
      readonly string? _subject = null;

      [XF] public void it_throws_AssertionFailedException_saying_the_custom_assertion_had_no_value_to_check() =>
         Invoking(() => _subject.Must().BeAnEmailAddress()).Must().Throw<AssertionFailedException>().Which.Message.Must().Contain("_subject was null, so BeAnEmailAddress had no value to check.");

      [XF] public void a_custom_failure_message_is_not_asked_for_a_message_about_the_null_value() =>
         Invoking(() => _subject.Must().BeAnEmailAddressWithItsOwnFailureMessage()).Must().Throw<AssertionFailedException>().Which.Message.Must().NotContain("is not an email address");
   }
}

static class TheCustomAssertionsThatNeedAValue
{
   ///<summary>Dereferences the value in its predicate, which a <see langword="null"/> one would make throw a <see cref="NullReferenceException"/>.</summary>
   internal static IAssertionContext<string> BeAnEmailAddress(this IAssertionContext<string?> context) =>
      context.RunAssertionThatANullValueFails(it => it.Contains('@', StringComparison.Ordinal));

   internal static IAssertionContext<string> BeAnEmailAddressWithItsOwnFailureMessage(this IAssertionContext<string?> context) =>
      context.RunAssertionThatANullValueFails(it => it.Contains('@', StringComparison.Ordinal), failureMessage: it => $"{it.Length} characters, and it is not an email address");
}
