namespace Compze.Must;

/// <summary>An expression's source text paired with the value it evaluated to, used to render assertion failure messages.</summary>
/// <param name="expression">The source text of the expression.</param>
/// <param name="value">The value the expression evaluated to.</param>
public class ExpressionValue(string expression, object? value)
{
   internal string Expression { get; } = expression;
   internal object? Value { get; } = value;
}
