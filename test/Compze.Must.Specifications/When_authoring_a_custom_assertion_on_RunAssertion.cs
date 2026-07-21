using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

///<summary>The custom-assertion extension point: an extension method on <see cref="IAssertionContext{T}"/> calling<br/>
/// <see cref="AssertionPrimitive.RunAssertion{T}"/> — exactly what <see cref="AssertionPrimitive"/>'s documentation tells a<br/>
/// consumer to write.</summary>
public class When_authoring_a_custom_assertion_on_RunAssertion : UniversalTestBase
{
   public class that_relies_on_the_default_failure_message : When_authoring_a_custom_assertion_on_RunAssertion
   {
      [XF] public void a_passing_value_flows_through_for_further_chaining() => 42.Must().BeTheAnswer().Be(42);

      [XF] public void a_failing_value_throws_with_the_heading_naming_the_custom_assertion() =>
         Invoking(() => 41.Must().BeTheAnswer())
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("BeTheAnswer");
   }

   public class that_builds_its_own_failure_message_from_the_AssertionCallInfo : When_authoring_a_custom_assertion_on_RunAssertion
   {
      readonly string _failureMessage =
         Invoking(() => 41.Must().BeTheAnswerDescribedByItsCallInfo(1))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

      [XF] public void the_info_names_the_calling_assertion_method() => _failureMessage.Must().Contain("BeTheAnswerDescribedByItsCallInfo");
      [XF] public void the_info_carries_the_predicate_source_text() => _failureMessage.Must().Contain("it => it == 42");
      [XF] public void the_info_carries_the_used_arguments() => _failureMessage.Must().Contain("1 argument(s)");
      [XF] public void the_info_hands_back_the_custom_failure_message_builder() => _failureMessage.Must().Contain("41 is not the answer");
   }
}

static class TheCustomAssertions
{
   internal static IAssertionContext<int> BeTheAnswer(this IAssertionContext<int> context) =>
      context.RunAssertion(it => it == 42);

   internal static IAssertionContext<int> BeTheAnswerDescribedByItsCallInfo(this IAssertionContext<int> context,
                                                                            int irrelevantArgument,
                                                                            [CallerArgumentExpression(nameof(irrelevantArgument))] string irrelevantArgumentExpression = null!) =>
      context.RunAssertion(it => it == 42,
                           messageOverride: info => $"{info.CallingMethod} failed: {info.FailureMessage!(context.Actual)} (predicate: {info.PredicateExpression}, {info.UsedArguments!.Count} argument(s))",
                           failureMessage: it => $"{it} is not the answer",
                           expressionValues: [new(irrelevantArgumentExpression, irrelevantArgument)]);
}
