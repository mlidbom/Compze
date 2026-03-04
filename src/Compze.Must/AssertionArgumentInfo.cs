namespace Compze.Must;

public class ExpressionValue(string expression, object? value)
{
   internal string Expression { get; } = expression;
   internal object? Value { get; } = value;
}
