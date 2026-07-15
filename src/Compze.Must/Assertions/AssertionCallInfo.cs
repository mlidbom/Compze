namespace Compze.Must;

// ReSharper disable UnusedAutoPropertyAccessor.Global
/// <summary>Details of a failing assertion, passed to the custom failure-message builder (the <c>messageOverride</c> argument of <see cref="AssertionPrimitive"/>'s RunAssertion).</summary>
/// <param name="predicateExpression">The source text of the predicate that failed.</param>
/// <param name="failureMessage">An optional function producing a custom failure message from the actual value.</param>
/// <param name="callingMethod">The name of the assertion method that performed the check.</param>
/// <param name="usedArguments">The expression/value pairs of the arguments involved, for rendering.</param>
public class AssertionCallInfo<T>(string predicateExpression, Func<T, string>? failureMessage, string callingMethod, IReadOnlyList<ExpressionValue>? usedArguments)
{
   /// <summary>The source text of the predicate that failed.</summary>
   public string PredicateExpression { get; } = predicateExpression;
   /// <summary>An optional function producing a custom failure message from the actual value.</summary>
   public Func<T, string>? FailureMessage { get; } = failureMessage;
   /// <summary>The name of the assertion method that performed the check.</summary>
   public string CallingMethod { get; } = callingMethod;
   /// <summary>The expression/value pairs of the arguments involved, for rendering.</summary>
   public IReadOnlyList<ExpressionValue>? UsedArguments { get; } = usedArguments;
}
